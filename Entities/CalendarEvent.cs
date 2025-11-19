using System.ComponentModel.DataAnnotations.Schema;

namespace AurionCal.Api.Entities;
public class CalendarEvent
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public required string ClassName { get; set; }
}
