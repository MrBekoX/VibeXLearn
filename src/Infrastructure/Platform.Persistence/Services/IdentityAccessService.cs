using Microsoft.EntityFrameworkCore;
using Platform.Application.Common.Interfaces;
using Platform.Persistence.Context;

namespace Platform.Persistence.Services;

/// <summary>
/// Identity read-access implementation for application business rules.
/// </summary>
public sealed class IdentityAccessService(AppDbContext db) : IIdentityAccessService
{
    private const string PermissionClaimType = "permission";

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct)
        => db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);

    public Task<bool> UserInRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);

        return (from ur in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id
                join user in db.Users.AsNoTracking() on ur.UserId equals user.Id
                where ur.UserId == userId &&
                      !user.IsDeleted &&
                      role.IsActive &&
                      role.NormalizedName == normalizedRoleName
                select ur).AnyAsync(ct);
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);

        return db.Roles
            .AsNoTracking()
            .AnyAsync(r => r.NormalizedName == normalizedRoleName && r.IsActive, ct);
    }

    public Task<bool> RoleHasPermissionAsync(string roleName, string permission, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);
        var normalizedPermission = NormalizePermission(permission);

        return (from role in db.Roles.AsNoTracking()
                join claim in db.RoleClaims.AsNoTracking() on role.Id equals claim.RoleId
                where role.NormalizedName == normalizedRoleName &&
                      role.IsActive &&
                      claim.ClaimType == PermissionClaimType &&
                      claim.ClaimValue == normalizedPermission
                select claim).AnyAsync(ct);
    }

    private static string NormalizeRoleName(string roleName)
        => roleName.Trim().ToUpperInvariant();

    private static string NormalizePermission(string permission)
        => permission.Trim().ToLowerInvariant();
}
