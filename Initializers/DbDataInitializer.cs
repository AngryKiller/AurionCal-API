using AurionCal.Api.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Initializers;

public class DbDataInitializer
{
    private readonly ApplicationDbContext _context;

    public DbDataInitializer(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Run()
    {
        _context.Database.Migrate();
       /* var isTemplateImported = _context.Settings.Any(x => x.Key == "template");

        if (!isTemplateImported) {
            var TemplateSetting = new Setting { Key = "template", Value = "default" };
            _context.Settings.Add(TemplateSetting);
            _context.SaveChanges();
        }*/
    }
}