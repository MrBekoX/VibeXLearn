using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Domain.Entities;

namespace Platform.Persistence.Configurations;

/// <summary>
/// Admin user ve roller için seed konfigürasyonu.
/// </summary>
public sealed class AdminUserConfiguration :
    IEntityTypeConfiguration<AppUser>,
    IEntityTypeConfiguration<AppRole>,
    IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    internal static readonly Guid AdminId          = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private  static readonly Guid AdminRoleId      = Guid.Parse("33333333-0000-0000-0000-000000000002");
    private  static readonly Guid InstructorRoleId = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private  static readonly Guid StudentRoleId    = Guid.Parse("33333333-0000-0000-0000-000000000004");

    private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        var hasher    = new PasswordHasher<AppUser>();
        var adminUser = new AppUser
        {
            Id                 = AdminId,
            UserName           = "admin@vibexlearn.com",
            NormalizedUserName = "ADMIN@VIBEXLEARN.COM",
            Email              = "admin@vibexlearn.com",
            NormalizedEmail    = "ADMIN@VIBEXLEARN.COM",
            EmailConfirmed     = true,
            FirstName          = "Platform",
            LastName           = "Admin",
            SecurityStamp      = "STATIC_SECURITY_STAMP_SEED_V1",
            ConcurrencyStamp   = "STATIC_CONCURRENCY_STAMP_SEED_V1",
            CreatedAt          = SeedDate
        };

        adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");
        // ⚠️ PRODUCTION'DA BU ŞİFRE MUTLAKA DEĞİŞTİRİLMELİDİR.

        builder.HasData(adminUser);
    }

    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.HasData(
            new
            {
                Id = AdminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "ROLE_ADMIN_STAMP_V1",
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = InstructorRoleId,
                Name = "Instructor",
                NormalizedName = "INSTRUCTOR",
                ConcurrencyStamp = "ROLE_INSTRUCTOR_STAMP_V1",
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = StudentRoleId,
                Name = "Student",
                NormalizedName = "STUDENT",
                ConcurrencyStamp = "ROLE_STUDENT_STAMP_V1",
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = (DateTime?)null
            }
        );
    }

    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.HasData(new IdentityUserRole<Guid>
        {
            UserId = AdminId,
            RoleId = AdminRoleId
        });
    }
}
