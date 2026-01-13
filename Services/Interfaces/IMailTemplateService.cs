namespace AurionCal.Api.Services.Interfaces;

public interface IMailTemplateService
{
    Task<string> RenderAsync<T>(string templateKey, T model);
}