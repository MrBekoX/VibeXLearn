using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Domain.Entities;
using Platform.Persistence.Configurations;

namespace Platform.Persistence.Services;

/// <summary>
/// Ensures the seeded admin user has a password on first startup.
/// Reads from <c>Identity:DefaultAdminPassword</c> configuration.
/// </summary>
public sealed class AdminPasswordInitializer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<AdminPasswordInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var password = config["Identity:DefaultAdminPassword"]
            ?? throw new InvalidOperationException(
                "Identity:DefaultAdminPassword is required. " +
                "Set it via environment variable or user-secrets.");

        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var admin = await userManager.FindByIdAsync(AdminUserConfiguration.AdminId.ToString());
        if (admin is null)
        {
            logger.LogWarning("Admin user seed not found; skipping password initialization");
            return;
        }

        if (admin.PasswordHash is not null)
        {
            logger.LogDebug("Admin user already has a password hash; skipping");
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(admin);
        var result = await userManager.ResetPasswordAsync(admin, token, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Failed to set admin password: {errors}");
        }

        logger.LogInformation("Admin user password initialized successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
