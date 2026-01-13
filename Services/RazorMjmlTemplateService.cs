using AurionCal.Api.Services.Interfaces;
using Mjml.Net;
using RazorLight;

namespace AurionCal.Api.Services;

public class RazorMjmlTemplateService(IMjmlRenderer mjmlRenderer) : IMailTemplateService
{
    private readonly RazorLightEngine _razorEngine = new RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Templates/Mail"))
        .UseMemoryCachingProvider()
        .Build();

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