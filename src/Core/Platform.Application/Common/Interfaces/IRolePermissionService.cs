using Platform.Application.Common.Results;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Role-permission management service based on identity role claims.
/// </summary>
public interface IRolePermissionService
{
    Task<Result> GrantPermissionAsync(string roleName, string permission, CancellationToken ct);
    Task<Result> RevokePermissionAsync(string roleName, string permission, CancellationToken ct);
    Task<IReadOnlyList<string>> GetPermissionsAsync(string roleName, CancellationToken ct);
}
