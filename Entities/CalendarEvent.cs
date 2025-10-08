using System.ComponentModel.DataAnnotations.Schema;

namespace AurionCal.Api.Entities;

public class CalendarEvent
{
    public string Id { get; set; }
    public string Title { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string ClassName { get; set; }
}