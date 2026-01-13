using System.Collections.Concurrent;
using AurionCal.Api.Contexts;
using AurionCal.Api.Services.Interfaces;
using Ical.Net.CalendarComponents;
using Microsoft.EntityFrameworkCore;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;
using AurionCal.Api.Services.Formatters;
using Ical.Net;
using Ical.Net.Serialization;
using AurionCal.Api.Entities;

namespace AurionCal.Api.Services;

public class CalendarService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEncryptionService _keyVaultService;
    private readonly ILogger<CalendarService> _logger;

    // Cache pour les SemaphoreSlim pour éviter de les recréer constamment
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public CalendarService(IEncryptionService keyVaultService, IServiceScopeFactory scopeFactory, ILogger<CalendarService> logger)
    {
        _keyVaultService = keyVaultService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RefreshCalendarEventsAsync(Guid userId, CancellationToken c)
    {
        var userLock = _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(c);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var scopedUser = await scopedDb.Users
                .Include(u => u.RefreshStatus)
                .FirstOrDefaultAsync(u => u.Id == userId, c);
            if (scopedUser == null) return;

            scopedUser.RefreshStatus ??= new UserRefreshStatus { UserId = scopedUser.Id };

            var now = DateTime.UtcNow;
            scopedUser.RefreshStatus.LastAttemptUtc = now;

            if (scopedUser.RefreshStatus.NextAttemptUtc.HasValue && scopedUser.RefreshStatus.NextAttemptUtc.Value > now)
            {
                await scopedDb.SaveChangesAsync(c);
                return;
            }

            var apiService = scope.ServiceProvider.GetRequiredService<MauriaApiService>();
            var notifier = scope.ServiceProvider.GetRequiredService<RefreshFailureNotifier>();

            var decryptedPass = await _keyVaultService.DecryptAsync(scopedUser.JuniaPassword, c);

            GetPlanningResponse? result;
            try
            {
                result = await apiService.GetPlanningAsync(scopedUser.JuniaEmail, decryptedPass, c);
            }
            catch (Exception ex)
            {
                await MarkFailureAsync(scopedDb, scopedUser, now, $"Exception: {ex.Message}", c);
                await NotifyIfThresholdReachedAsync(scopedDb, scopedUser, notifier, c);
                return;
            }

            if (result is { Success: true, Data: { } rawEvents })
            {
                await using var tx = await scopedDb.Database.BeginTransactionAsync(c);

                await scopedDb.CalendarEvents
                    .Where(e => e.UserId == scopedUser.Id)
                    .ExecuteDeleteAsync(c);

                var newEvents = rawEvents
                    .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                    .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Select(e => new CalendarEvent
                    {
                        Id = e.Id.Trim(),
                        Title = e.Title.Trim(),
                        Start = e.Start.ToUniversalTime(),
                        End = e.End.ToUniversalTime(),
                        ClassName = e.ClassName,
                        UserId = scopedUser.Id // utiliser la FK plutôt que l'entité
                    })
                    .ToList();

                if (newEvents.Count > 0)
                {
                    await scopedDb.CalendarEvents.AddRangeAsync(newEvents, c);
                }

                scopedUser.LastUpdate = now;

                // success: reset status
                scopedUser.RefreshStatus.ConsecutiveFailureCount = 0;
                scopedUser.RefreshStatus.LastSuccessUtc = now;
                scopedUser.RefreshStatus.LastFailureUtc = null;
                scopedUser.RefreshStatus.LastFailureReason = null;
                scopedUser.RefreshStatus.NextAttemptUtc = null;
                scopedUser.RefreshStatus.FailureEmailSentUtc = null;

                await scopedDb.SaveChangesAsync(c);
                await tx.CommitAsync(c);
            }
            else
            {
                var reason = result == null
                    ? "Planning response null"
                    : result.Success == false
                        ? "Mauria returned Success=false"
                        : "Planning missing data";

                await MarkFailureAsync(scopedDb, scopedUser, now, reason, c);
                await NotifyIfThresholdReachedAsync(scopedDb, scopedUser, notifier, c);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement pour l'utilisateur {UserId}", userId);
        }
        finally
        {
            userLock.Release();
        }
    }

    private static async Task MarkFailureAsync(ApplicationDbContext db, User user, DateTime nowUtc, string reason, CancellationToken c)
    {
        user.RefreshStatus ??= new UserRefreshStatus { UserId = user.Id };

        user.RefreshStatus.ConsecutiveFailureCount++;
        user.RefreshStatus.LastFailureUtc = nowUtc;
        user.RefreshStatus.LastFailureReason = Truncate(reason, 500);
        user.RefreshStatus.NextAttemptUtc = nowUtc + ComputeBackoff(user.RefreshStatus.ConsecutiveFailureCount);

        await db.SaveChangesAsync(c);
    }

    private static async Task NotifyIfThresholdReachedAsync(ApplicationDbContext db, User user, RefreshFailureNotifier notifier, CancellationToken c)
    {
        // à voir si rendre paramétrable ou non...
        if (user.RefreshStatus?.FailureEmailSentUtc != null)
            return;

        const int threshold = 3;
        if (user.RefreshStatus is not { ConsecutiveFailureCount: >= threshold })
            return;

        await notifier.SendDataFetchErrorAsync(user.JuniaEmail, user.LastUpdate, c);
        user.RefreshStatus!.FailureEmailSentUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(c);
    }

    private static TimeSpan ComputeBackoff(int consecutiveFailures)
    {
        // Idem ci-dessus
        return consecutiveFailures switch
        {
            <= 1 => TimeSpan.FromHours(1),
            2 => TimeSpan.FromHours(3),
            3 => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(12)
        };
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    public string GenerateCalendarFeed(IEnumerable<CalendarEvent> planningEvents)
    {
        var calendar = new Calendar();
        calendar.AddTimeZone(new VTimeZone("Europe/Paris"));

        foreach (var evt in planningEvents)
        {
            calendar.Events.Add(CalendarEventFormatter.ToIcalEvent(evt));
        }

        return new CalendarSerializer().SerializeToString(calendar);
    }
}