using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Common;
using Platform.Domain.Entities;

namespace Platform.Persistence.Context;

/// <summary>
/// Ana uygulama DbContext'i. Npgsql + PostgreSQL için optimize edilmiştir.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDomainEventDispatcher? domainEventDispatcher = null)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Category>        Categories      => Set<Category>();
    public DbSet<Course>          Courses         => Set<Course>();
    public DbSet<Lesson>          Lessons         => Set<Lesson>();
    public DbSet<LiveSession>     LiveSessions    => Set<LiveSession>();
    public DbSet<Submission>      Submissions     => Set<Submission>();
    public DbSet<Enrollment>      Enrollments     => Set<Enrollment>();
    public DbSet<Order>           Orders          => Set<Order>();
    public DbSet<PaymentIntent>   PaymentIntents  => Set<PaymentIntent>();
    public DbSet<Coupon>          Coupons         => Set<Coupon>();
    public DbSet<Badge>           Badges          => Set<Badge>();
    public DbSet<UserBadge>       UserBadges      => Set<UserBadge>();
    public DbSet<Certificate>     Certificates    => Set<Certificate>();
    public DbSet<RefreshToken>    RefreshTokens   => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tüm IEntityTypeConfiguration sınıflarını otomatik yükle
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global: tüm DateTime kolonlarını UTC'ye normalize et
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                prop.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }
        }

        // Identity tablo isimlerini short yap
        modelBuilder.Entity<AppUser>(b => b.ToTable("users"));
        modelBuilder.Entity<AppRole>(b => b.ToTable("roles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        SetAuditFields();

        var domainEntities = ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.Entity is IAggregateRoot && x.Entity.DomainEvents.Count != 0)
            .Select(x => x.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);

        if (domainEventDispatcher is null || domainEntities.Count == 0)
            return result;

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var entity in domainEntities)
            entity.ClearDomainEvents();

        await domainEventDispatcher.DispatchAllAsync(domainEvents, ct);
        return result;
    }

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedAtForPersistence(now);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedAtForPersistence(now);
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;

                case EntityState.Deleted:
                    // Hard delete'i soft delete'e çevir
                    entry.State = EntityState.Modified;
                    entry.Entity.SetDeletedForPersistence(now);
                    break;
            }
        }
    }
}
