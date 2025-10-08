using AurionCal.Api.Services;
using FastEndpoints;

namespace AurionCal.Api.Endpoints;

public class CheckLoginInfoRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class CheckLoginInfoEndpoint(MauriaApiService apiService)
    : Endpoint<CheckLoginInfoRequest, CheckLoginInfoResponse>
{
    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/check-aurion-auth");
    }
    
    public override async Task HandleAsync(CheckLoginInfoRequest r, CancellationToken c)
    {
        var result = await apiService.CheckLoginInfoAsync(r.Email, r.Password, c);

        if (result.Success)
        {
            await Send.ResponseAsync(
                new CheckLoginInfoResponse() { IsValid = result.Success, Message = result.Error ?? "" }, 200, c);
        }
        else
        {
            await Send.UnauthorizedAsync(c);
        }

    }
    
}

public class CheckLoginInfoResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
}