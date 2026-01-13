using AurionCal.Api.Contexts;
using AurionCal.Api.Initializers;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using MailKitSimplified.Sender;
using Microsoft.EntityFrameworkCore;
using Mjml.Net;

var bld = WebApplication.CreateBuilder();

var jwtSection = bld.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey") ?? throw new InvalidOperationException("Jwt:SigningKey manquant");

bld.Services.AddTransient<HttpClientHandler>();
bld.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(bld.Configuration.GetConnectionString("ApplicationDbContext")));
bld.Services.AddTransient<DbDataInitializer>();
bld.Services.AddHttpClient<MauriaApiService>();
bld.Services.AddScoped<CalendarService>();
bld.Services.AddScoped<RefreshFailureNotifier>();
bld.Services.AddMemoryCache();
bld.Services.AddMailKitSimplifiedEmailSender(bld.Configuration);
bld.Services.AddSingleton<IMjmlRenderer, MjmlRenderer>();
bld.Services.AddScoped<IEmailSenderService, SmtpSenderService>();
bld.Services.AddScoped<IMailTemplateService, RazorMjmlTemplateService>();

var keyVaultUrl = bld.Configuration.GetSection("KeyVault").GetValue<string>("KeyVaultUrl");
if (!string.IsNullOrWhiteSpace(keyVaultUrl))
{
    bld.Services.AddScoped<IEncryptionService, KeyVaultService>();
}
else
{
    bld.Services.AddScoped<IEncryptionService, LocalEncryptionService>();
}

bld.Services.AddAuthenticationJwtBearer(s =>
{
    s.SigningKey = signingKey;
});

bld.Services.AddAuthorization()
    .AddFastEndpoints()
    .SwaggerDocument();


// Définition des politiques CORS
bld.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllDev", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    options.AddPolicy("AllowSpecificProd", policy =>
    {
        policy.WithOrigins(bld.Configuration.GetSection("ApiSettings").GetValue<string>("Cors")!)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = bld.Build();

app.UseCors(app.Environment.IsDevelopment() ? "AllowAllDev" : "AllowSpecificProd");


using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var initializer = services.GetRequiredService<DbDataInitializer>();
initializer.Run();
app.UseAuthentication().UseAuthorization().UseFastEndpoints().UseSwaggerGen();
app.UseSwaggerUi();
app.Run();