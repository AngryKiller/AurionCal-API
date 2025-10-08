namespace AurionCal.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public string JuniaEmail { get; set; }
    public string JuniaPassword { get; set; }
    public DateTime? LastUpdate { get; set; }
    public virtual List<CalendarEvent> Planning { get; set; }
    public Guid CalendarToken { get; set; }
}