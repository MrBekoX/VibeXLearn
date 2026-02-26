namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Read-only access methods for identity data needed by application rules.
/// </summary>
public interface IIdentityAccessService
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct);
    Task<bool> UserInRoleAsync(Guid userId, string roleName, CancellationToken ct);
    Task<bool> RoleExistsAsync(string roleName, CancellationToken ct);
    Task<bool> RoleHasPermissionAsync(string roleName, string permission, CancellationToken ct);
}
