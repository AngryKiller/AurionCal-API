
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
    
    
    public static void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasKey(e => new { e.Id, e.UserId });
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired();
        builder.Property(e => e.ClassName).IsRequired();
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
    }
}
