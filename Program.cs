using AurionCal.Api.Contexts;
using AurionCal.Api.Initializers;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

var bld = WebApplication.CreateBuilder();
bld.Services.AddTransient<HttpClientHandler>();
bld.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(bld.Configuration.GetConnectionString("ApplicationDbContext")));
bld.Services.AddTransient<DbDataInitializer>();
bld.Services.AddHttpClient<MauriaApiService>();
bld.Services.AddScoped<CalendarService>();
bld.Services.AddScoped<IEncryptionService, KeyVaultService>();
bld.Services.AddFastEndpoints().SwaggerDocument();

var app = bld.Build();
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var initializer = services.GetRequiredService<DbDataInitializer>();
initializer.Run();
app.UseFastEndpoints().UseSwaggerGen();
app.UseSwaggerUi();
app.Run();