---
name: ef-migrations-setup
description: Migrations klasörü boş. İlk migration oluşturulmamış. ApplyMigrationsAsync çağrılıyor ama uygulanacak migration yok. Bu skill, EF Core migrations altyapısını kurar ve ilk migration'ı oluşturur.
---

# Setup EF Core Migrations

## Problem

**Risk Level:** MEDIUM

- `Migrations/` klasörü boş
- İlk migration oluşturulmamış
- `ApplyMigrationsAsync` çağrılıyor ama uygulanacak migration yok
- Veritabanı şeması versiyon kontrolünde takip edilemiyor

**Affected Files:**
- `src/Infrastructure/Platform.Persistence/Migrations/`
- `src/Infrastructure/Platform.Persistence/Context/AppDbContextFactory.cs`

## Solution Steps

### Step 1: Verify AppDbContextFactory Exists

Check: `src/Infrastructure/Platform.Persistence/Context/AppDbContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Platform.Persistence.Context;

/// <summary>
/// Design-time factory for EF Core CLI tools.
/// Required for `dotnet ef` commands.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                "../Presentation/Platform.WebAPI"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found");

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            npgsql.UseSnakeCaseNamingConvention();
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
```

### Step 2: Install EF Core Tools (if not installed)

```bash
# Install EF Core CLI tool globally
dotnet tool install --global dotnet-ef

# Or update to latest
dotnet tool update --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Step 3: Create Initial Migration

```bash
# Navigate to WebAPI project
cd src/Presentation/Platform.WebAPI

# Create initial migration
dotnet ef migrations add InitialCreate \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project . \
    --output-dir Migrations

# Expected output:
# Build started...
# Build succeeded.
# Done. To undo this action, use 'ef migrations remove'
```

### Step 4: Review Generated Migration

Check the generated files in `src/Infrastructure/Platform.Persistence/Migrations/`:
- `YYYYMMDDHHMMSS_InitialCreate.cs` - Main migration
- `YYYYMMDDHHMMSS_InitialCreate.Designer.cs` - Designer file
- `AppDbContextModelSnapshot.cs` - Current model snapshot

### Step 5: Apply Migration to Database

```bash
# Apply to local database
dotnet ef database update \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .

# Expected output:
# Build started...
# Build succeeded.
# Applying migration '20240101_InitialCreate'.
# Done.
```

### Step 6: Verify Database Schema

```bash
# Connect to PostgreSQL
psql -h localhost -U vibex_user -d vibexlearn

# List tables
\dt

# Expected output:
#  public | __efmigrationshistory | table | vibex_user
#  public | users                 | table | vibex_user
#  public | courses               | table | vibex_user
#  public | orders                | table | vibex_user
#  ... etc

# Check migration history
SELECT * FROM "__efmigrationshistory";
```

### Step 7: Configure Auto-Migration (Development Only)

Update `src/Presentation/Platform.WebAPI/Program.cs`:

```csharp
// Auto-apply migrations in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        app.Logger.LogInformation(
            "Applying {Count} pending migrations...",
            pendingMigrations.Count());

        await dbContext.Database.MigrateAsync();

        app.Logger.LogInformation("Migrations applied successfully");
    }
}
// NOTE: In Production, migrations should be applied via CI/CD pipeline
```

### Step 8: Add Migration Scripts for Production

Create: `scripts/apply-migrations.sh`

```bash
#!/bin/bash
set -e

echo "Applying EF Core migrations..."

cd src/Presentation/Platform.WebAPI

# Get connection string from environment
if [ -z "$ConnectionStrings__DefaultConnection" ]; then
    echo "Error: ConnectionStrings__DefaultConnection not set"
    exit 1
fi

# Apply migrations
dotnet ef database update \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project . \
    --verbose

echo "Migrations applied successfully!"
```

### Step 9: Add Migration to CI/CD

Add to `.github/workflows/deploy.yml`:

```yaml
- name: Apply Migrations
  run: |
    cd src/Presentation/Platform.WebAPI
    dotnet ef database update \
      --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
      --startup-project .
  env:
    ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION_STRING }}
```

### Step 10: Create Future Migrations

When making schema changes:

```bash
# After modifying entities
cd src/Presentation/Platform.WebAPI

# Create new migration
dotnet ef migrations add AddNewFeature \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .

# Review the generated migration
cat ../../Infrastructure/Platform.Persistence/Migrations/*_AddNewFeature.cs

# Apply to local database
dotnet ef database update \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .

# If migration is wrong, remove it (before applying!)
dotnet ef migrations remove \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .
```

## Verification

```bash
# 1. Check migration exists
ls -la src/Infrastructure/Platform.Persistence/Migrations/

# 2. Check database tables
psql -h localhost -U vibex_user -d vibexlearn -c "\dt"

# 3. Run application
dotnet run --project src/Presentation/Platform.WebAPI

# 4. Check logs for migration messages
# Should see: "Migrations applied successfully" or "No pending migrations"
```

## Troubleshooting

```bash
# If migration fails, reset database (DEVELOPMENT ONLY!)
dotnet ef database drop --force \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .

dotnet ef database update \
    --project ../../Infrastructure/Platform.Persistence/Platform.Persistence.csproj \
    --startup-project .
```

## Priority

**SHORT-TERM** - Required for database schema management.
