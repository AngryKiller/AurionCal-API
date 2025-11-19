using System.Collections.Concurrent;
using AurionCal.Api.Contexts;
using AurionCal.Api.Services.Interfaces;
using Ical.Net.CalendarComponents;
using Microsoft.EntityFrameworkCore;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;

namespace AurionCal.Api.Services;

public class CalendarService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEncryptionService _keyVaultService;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(IEncryptionService keyVaultService, IServiceScopeFactory scopeFactory, ILogger<CalendarService> logger)
    {
        _keyVaultService = keyVaultService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks 
        = new ConcurrentDictionary<Guid, SemaphoreSlim>();

    public async Task RefreshCalendarEventsAsync(Guid UserId, CancellationToken c)
    {
        var userLock = _locks.GetOrAdd(UserId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(c);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var tx = await scopedDb.Database.BeginTransactionAsync(c);
            var scopedApiService = scope.ServiceProvider.GetRequiredService<MauriaApiService>();
            var scopedUser = await scopedDb.Users.FirstOrDefaultAsync(u => u.Id == UserId, c);
            if (scopedUser == null) return;
            var events = await scopedApiService.GetPlanningAsync(scopedUser.JuniaEmail,
                await _keyVaultService.DecryptAsync(scopedUser.JuniaPassword, c), c);
            if (events is { Success: true, Data: not null })
            {
                await scopedDb.CalendarEvents.Where(e => e.UserId == scopedUser.Id).ExecuteDeleteAsync(c);
                foreach (var entry in scopedDb.ChangeTracker.Entries<CalendarEvent>().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                var newEvents = events.Data
                    .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                    .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Select(e => new CalendarEvent
                    {
                        Id = e.Id.Trim(),
                        Title = e.Title,
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

                scopedUser.LastUpdate = DateTime.UtcNow;
                await scopedDb.SaveChangesAsync(c);
                await tx.CommitAsync(c);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflit de concurrence lors du rafraîchissement asynchrone du planning pour l'utilisateur {UserId}",
                UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement asynchrone du planning pour l'utilisateur {UserId}",
                UserId);
        }
        finally
        {
            userLock.Release();
        }
    }
    
    public string GenerateCalendarFeed(List<CalendarEvent> planningEvents)
    {
        var calendar = new Ical.Net.Calendar();
        calendar.AddTimeZone(new VTimeZone("Europe/Paris"));
        foreach (var planningEvent in planningEvents)
        {
            var calendarEvent = new Ical.Net.CalendarComponents.CalendarEvent
            {
                Summary = planningEvent.Title,
                Start = new Ical.Net.DataTypes.CalDateTime(planningEvent.Start.DateTime).ToTimeZone("Europe/Paris"),
                End = new Ical.Net.DataTypes.CalDateTime(planningEvent.End.DateTime).ToTimeZone("Europe/Paris"),
                Description = planningEvent.Title,
                Location = planningEvent.ClassName,
                Uid = planningEvent.Id
            };
            calendar.Events.Add(calendarEvent);
        }
        var serializer = new Ical.Net.Serialization.CalendarSerializer();
        return serializer.SerializeToString(calendar)!;
    }
}