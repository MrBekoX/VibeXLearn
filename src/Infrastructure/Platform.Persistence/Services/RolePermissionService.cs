using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Persistence.Context;

namespace Platform.Persistence.Services;

/// <summary>
/// Role-permission operations implemented via Identity role claims.
/// </summary>
public sealed class RolePermissionService(AppDbContext db) : IRolePermissionService
{
    private const string PermissionClaimType = "permission";

    public async Task<Result> GrantPermissionAsync(string roleName, string permission, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);
        var normalizedPermission = NormalizePermission(permission);

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, ct);

        if (role is null)
            return Result.Fail("ROLE_NOT_FOUND", "Role not found.");

        if (!role.IsActive)
            return Result.Fail("ROLE_INACTIVE", "Role is inactive.");

        var alreadyExists = await db.RoleClaims.AnyAsync(
            c => c.RoleId == role.Id &&
                 c.ClaimType == PermissionClaimType &&
                 c.ClaimValue == normalizedPermission, ct);

        if (alreadyExists)
            return Result.Fail("ROLE_PERMISSION_EXISTS",
                "Permission is already granted to this role.");

        await db.RoleClaims.AddAsync(new IdentityRoleClaim<Guid>
        {
            RoleId = role.Id,
            ClaimType = PermissionClaimType,
            ClaimValue = normalizedPermission
        }, ct);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RevokePermissionAsync(string roleName, string permission, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);
        var normalizedPermission = NormalizePermission(permission);

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, ct);

        if (role is null)
            return Result.Fail("ROLE_NOT_FOUND", "Role not found.");

        if (!role.IsActive)
            return Result.Fail("ROLE_INACTIVE", "Role is inactive.");

        var claim = await db.RoleClaims.FirstOrDefaultAsync(
            c => c.RoleId == role.Id &&
                 c.ClaimType == PermissionClaimType &&
                 c.ClaimValue == normalizedPermission, ct);

        // Idempotent revoke.
        if (claim is null)
            return Result.Success();

        db.RoleClaims.Remove(claim);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(string roleName, CancellationToken ct)
    {
        var normalizedRoleName = NormalizeRoleName(roleName);

        var role = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, ct);

        if (role is null || !role.IsActive)
            return [];

        return await db.RoleClaims
            .AsNoTracking()
            .Where(c => c.RoleId == role.Id && c.ClaimType == PermissionClaimType && c.ClaimValue != null)
            .Select(c => c.ClaimValue!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
    }

    private static string NormalizeRoleName(string roleName)
        => roleName.Trim().ToUpperInvariant();

    private static string NormalizePermission(string permission)
        => permission.Trim().ToLowerInvariant();
}
