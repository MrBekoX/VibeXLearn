using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Platform.Persistence.Context;

/// <summary>
/// Design-time factory for EF Core CLI commands (migrations).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidateBasePaths = new[]
        {
            Path.GetFullPath(Path.Combine(currentDirectory, "../../Presentation/Platform.WebAPI")),
            Path.GetFullPath(Path.Combine(currentDirectory, "../Presentation/Platform.WebAPI")),
            Path.GetFullPath(Path.Combine(currentDirectory, "src/Presentation/Platform.WebAPI"))
        };

        var startupBasePath = candidateBasePaths.FirstOrDefault(Directory.Exists)
            ?? throw new DirectoryNotFoundException(
                "Cannot resolve Presentation/Platform.WebAPI path for design-time DbContext factory.");

        var config = new ConfigurationBuilder()
            .SetBasePath(startupBasePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var opt = new DbContextOptionsBuilder<AppDbContext>();
        opt.UseNpgsql(config.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention();

        return new AppDbContext(opt.Options);
    }
}
