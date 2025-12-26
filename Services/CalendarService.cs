using System.Collections.Concurrent;
using AurionCal.Api.Contexts;
using AurionCal.Api.Services.Interfaces;
using Ical.Net.CalendarComponents;
using Microsoft.EntityFrameworkCore;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;
using AurionCal.Api.Enums;
using AurionCal.Api.Services.Formatters;
using Ical.Net;
using Ical.Net.Serialization;

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
            await using var tx = await scopedDb.Database.BeginTransactionAsync(c);
            
            var scopedUser = await scopedDb.Users.FirstOrDefaultAsync(u => u.Id == userId, c);
            if (scopedUser == null) return;

            var apiService = scope.ServiceProvider.GetRequiredService<MauriaApiService>();
            var decryptedPass = await _keyVaultService.DecryptAsync(scopedUser.JuniaPassword, c);
            
            var result = await apiService.GetPlanningAsync(scopedUser.JuniaEmail, decryptedPass, c);

            if (result is { Success: true, Data: { } rawEvents })
            {
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

                scopedUser.LastUpdate = DateTime.UtcNow;
                await scopedDb.SaveChangesAsync(c); 
                await tx.CommitAsync(c);
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