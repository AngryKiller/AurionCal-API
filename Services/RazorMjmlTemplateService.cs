using AurionCal.Api.Services.Interfaces;
using Mjml.Net;
using RazorLight;

namespace AurionCal.Api.Services;

public class RazorMjmlTemplateService(IMjmlRenderer mjmlRenderer) : IMailTemplateService
{
    private readonly RazorLightEngine _razorEngine = BuildEngine();

    private static RazorLightEngine BuildEngine()
    {
        // En prod (docker publish), Directory.GetCurrentDirectory() peut varier.
        // AppContext.BaseDirectory pointe vers le répertoire où se trouve l'app publiée (/app).
        var root = Path.Combine(AppContext.BaseDirectory, "Templates", "Mail");

        if (!Directory.Exists(root))
        {
            // fallback utile en dev / certains scénarios de lancement
            var alt = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Mail");
            if (Directory.Exists(alt))
                root = alt;
        }

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Root directory {root} not found. Ensure Templates are copied to output/publish (Templates/**). BaseDirectory={AppContext.BaseDirectory}; CWD={Directory.GetCurrentDirectory()}");

        return new RazorLightEngineBuilder()
            .UseFileSystemProject(root)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync<T>(string templateFilename, T model)
    {
        string mjmlString = await _razorEngine.CompileRenderAsync(templateFilename, model);

        var renderResult = await mjmlRenderer.RenderAsync(mjmlString);

        if (!renderResult.Errors.Any())
        {
            return renderResult.Html;
        }

        throw new Exception($"Erreur MJML: {string.Join(", ", renderResult.Errors.Select(e => e.Error))}");
    }
}