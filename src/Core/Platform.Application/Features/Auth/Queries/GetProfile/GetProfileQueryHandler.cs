using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Auth.DTOs;

namespace Platform.Application.Features.Auth.Queries.GetProfile;

/// <summary>
/// Handler for GetProfileQuery.
/// </summary>
public sealed class GetProfileQueryHandler(
    IAuthService authService) : IRequestHandler<GetProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        return await authService.GetProfileAsync(request.UserId, ct);
    }
}
