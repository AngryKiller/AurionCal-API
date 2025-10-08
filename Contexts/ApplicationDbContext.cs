using Microsoft.EntityFrameworkCore;
using AurionCal.Api.Entities;

namespace AurionCal.Api.Contexts;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("ApplicationDbContext");
        }
    }
}