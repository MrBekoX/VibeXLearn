using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Auth.DTOs;

namespace Platform.Application.Features.Auth.Queries.GetProfile;

/// <summary>
/// Query to get the authenticated user's profile.
/// </summary>
public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;
