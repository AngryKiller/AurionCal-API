using AurionCal.Api.Schools;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Turns a raw Aurion event into an iCal event, following a school's conventions.
/// </summary>
public interface IEventFormatter
{
    /// <summary>Whether the event belongs in the feed for the given options (some events can be hidden by the user).</summary>
    bool IsVisible(CalendarEvent evt, FormatOptions options);

    IcalEvent Format(CalendarEvent evt, FormatOptions options);
}

/// <summary>
/// Provides a school's formatter (based on <see cref="School.ParserId"/>).
/// </summary>
public interface IEventFormatterFactory
{
    IEventFormatter For(School school);
}
