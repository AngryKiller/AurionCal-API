using Ical.Net.CalendarComponents;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;

namespace AurionCal.Api.Services;

public class CalendarService
{


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