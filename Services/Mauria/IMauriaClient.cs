using AurionCal.Api.Schools;

namespace AurionCal.Api.Services;

/// <summary>
/// Client of the Mauria service. The school provides the Aurion URL to query.
/// </summary>
public interface IMauriaClient
{
    Task<CheckLoginInfoResponse> CheckLoginAsync(School school, string email, string password, CancellationToken c = default);

    Task<GetPlanningResponse> GetPlanningAsync(School school, string email, string password, CancellationToken c = default);
}
