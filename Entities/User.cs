namespace AurionCal.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string SchoolId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime? LastUpdate { get; set; }
    public virtual List<CalendarEvent> Planning { get; set; }
    public Guid CalendarToken { get; set; }
    public bool ExamAccommodations { get; set; }
    public virtual UserRefreshStatus? RefreshStatus { get; set; }
}