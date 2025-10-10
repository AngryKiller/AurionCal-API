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

    public async Task RefreshCalendarEventsAsync(Guid UserId, CancellationToken c)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scopedApiService = scope.ServiceProvider.GetRequiredService<MauriaApiService>();
            // Reload user
            var scopedUser = await scopedDb.Users.Include(u => u.Planning).FirstOrDefaultAsync(u => u.Id == UserId, c);
            if (scopedUser == null) return;
            var events = await scopedApiService.GetPlanningAsync(scopedUser.JuniaEmail, await _keyVaultService.DecryptAsync(scopedUser.JuniaPassword, c), c);
            await scopedDb.CalendarEvents.Where(e => e.User.Id == scopedUser.Id).ExecuteDeleteAsync(c);
            if (events is { Success: true, Data: not null })
            {
                scopedUser.Planning = events.Data?.Select(e => new CalendarEvent
                {
                    Id = e.Id,
                    Title = e.Title,
                    Start = e.Start.ToUniversalTime(),
                    End = e.End.ToUniversalTime(),
                    ClassName = e.ClassName,
                }).ToList()!;
                scopedUser.LastUpdate = DateTime.Now.ToUniversalTime();
                await scopedDb.SaveChangesAsync(c);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement asynchrone du planning pour l'utilisateur {UserId}", UserId);
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