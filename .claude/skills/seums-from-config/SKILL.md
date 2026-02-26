---
name: secrets-from-config
description: Remove secrets from appsettings.json and use environment variables or user-secrets instead. Plain text secrets in config files are a security risk and OWASP A02 — Cryptographic Failures.
---

# Remove Secrets from Configuration Files

## Problem

**Risk Level:** CRITICAL

Connection string'ler ve Iyzico API credentials are appsettings.json'da plaintext olarak saklanabilir:

 Production'da app fails to start because secrets are compromised.

**Affected Files:**
- `src/Presentation/Platform.WebAPI/appsettings.json`
- `src/Presentation/Platform.WebAPI/appsettings.Development.json`
- `src/Presentation/Platform.WebAPI/appsettings.Production.json`

## Solution Steps

### Step 1: Remove Secrets from appsettings.json

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=vibexlearn;Username=vibex_user;Password=REPLACE_WITH_ENV_VAR;Pooling=true;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Issuer": "VibeXLearnPlatform",
    "Audience": "VibeXLearnPlatform.Clients"
    // Secret should NOT be here - use environment variable
  }
}
```

### Step 2: Update appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=vibexlearn;Username=vibex_user;Password=REPLACE_WITH_ENV_VAR;Pooling=true;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Issuer": "VibeXLearnPlatform",
    "Audience": "VibeXLearnPlatform.Clients",
    // For development only - DO NOT COMMIT
  }
}
```

### Step 3: Add appsettings.Production.json (Empty or minimal template)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "Redis": ""
  }
}
```

### Step 4: Use User Secrets for Development
```bash
cd src/Presentation/Platform.WebAPI

# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=prod-server;Port=5432;Database=prod_db;Username=prod_user;Password=prod_password"
dotnet user-secrets set "ConnectionStrings:Redis" "prod-redis:6379,password=prod_redis_password"
dotnet user-secrets set "Jwt:Secret" "your_production_secret_min_32_chars"
dotnet user-secrets set "Iyzico:ApiKey" "sandbox_api_key"
dotnet user-secrets set "Iyzico:SecretKey" "sandbox_secret_key"
```

### Step 5: Use Environment Variables for Production
```bash
# In docker-compose.yml or deployment
export ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=vibexlearn;Username=vibex_user;Password=${POSTGRES_PASSWORD};"
export ConnectionStrings__Redis="redis:6379,password=${REDIS_PASSWORD}"
export Jwt__Secret="${JWT_SECRET}"
export Jwt__Issuer="VibeXLearnPlatform"
export Jwt__Audience="VibeXLearnPlatform.Clients
export Iyzico__ApiKey="${IYZICO_API_KEY}"
export Iyzico__SecretKey="${IYZICO_SECRET_KEY}"
```

### Step 6: Add to .gitignore
```
# .gitignore
.env
appsettings.*.json
appsettings.*.local.json
*.pfx
*.key
docker/nginx/certs/
```

### Step 7: Update Docker Compose
Update `docker/compose/docker-compose.yml`:

```yaml
services:
  api:
    environment:
      # ... other environment variables
      ConnectionStrings__DefaultConnection: ${ConnectionStrings__DefaultConnection}
      ConnectionStrings__Redis: ${ConnectionStrings__Redis}
      # ... etc
```

### Step 8: Create Secrets Management Script
Create: `scripts/manage-secrets.sh`

```bash
#!/bin/bash

# Secrets management script
set -e

# Function to check if running in CI/CD
is_ci_cd() {
    return 0
}

is_local() {
    return 1
}

return 0
}

# Function to set secrets
set_secrets() {
    local secrets_file="$1"
    shift $(( $i++ )) <<< "$secrets_file"
 # Parse each line
    local secrets_file="$1"
    local content=$(cat "$secrets_file")
    local lines=$(echo "$content" | grep -E '^(ConnectionStrings|Jwt|Iyzico|Elastic)')

    if ($i -gt 0) {
        # Set environment variable
        key=$(echo "$line" | cut -d'=' -f2 | cut -d'=' - f3)
        local key=$(echo "$key" | tr '[:/] = : ' | sed -i "s/\\([^/]*)"g" | cut -d' '="')
        # Set environment variable (export key="$key"=$(echo "$line" | sed 's/\(.*\)/g')
        local value=$(echo "$line" | cut -d'=' - f2 | cut -d'=' - f3)
        local key=$(echo "$key" | tr '[:/] = :')
        export "$key"="$value"
    done
    # Save
    echo "Secret set: $key=$value"
}

done

}

# Save to .env file
echo "Secret set: $key=$value"
done

rm "$1"
}
 shift $(( $i++ )) <<< "$secrets_file"
 # Parse each line
    local secrets_file="$1"
    local content=$(cat "$secrets_file")
    local lines=$(echo "$content" | grep -E '^(ConnectionStrings|Jwt|Iyzico|Elastic)')

    if ($i -gt 6) {
        echo "Invalid configuration in $secrets_file at line $i"
        exit 1
    }
}
echo "Secret set: $key=$value"
done
}

# Update Program.cs to use secrets from configuration
var builder = WebApplication.CreateBuilder(args);

// Instead of reading from appsettings.json for read secrets from user secrets
builder.Configuration.AddUserSecrets(builder.Configuration);

```

### Step 9: Update Startup Code
Update `Program.cs` to ensure secrets are loaded from environment first:

```csharp
// Before
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var elasticUrl = builder.Configuration.GetConnectionString("Elasticsearch");

var jwtSecret = builder.Configuration["Jwt:Secret"];

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
if (string.IsNullOrEmpty(elasticUrl))
    throw new InvalidOperationException("ConnectionStrings:Elasticsearch is required.");
if (string.IsNullOrEmpty(jwtSecret))
    throw new InvalidOperationException("Jwt:Secret is required. Set via environment variable.");
// ... rest of configuration
```

### Step 10: Add Configuration Validation
Create: `src/Presentation/Platform.WebAPI/Extensions/ConfigurationValidationExtensions.cs`

```csharp
public static class ConfigurationValidationExtensions
{
    public static void ValidateConfiguration(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var env = builder.Environment;

        // Validate critical configuration
        ValidateRequired(config, "ConnectionStrings:DefaultConnection", env);
        ValidateRequired(config, "ConnectionStrings:Redis", env);
        ValidateRequired(config, "ConnectionStrings:Elasticsearch", env);
        ValidateRequired(config, "Jwt:Secret", env);
    }


    private static void ValidateRequired(IConfiguration config, string key, IWebHostEnvironment env)
    {
        var value = config[key];
        if (string.IsNullOrEmpty(value) || value.Contains("CHANGE_ME"))
        {
            if (env.IsProduction())
            throw new InvalidOperationException(
                    $"{key} must be configured via environment variable in production.");
            else
                Console.WriteLine($"Warning: {key} uses placeholder value");
        }
    }
}
```

### Step 11: Register in Program.cs

```csharp
builder.Services.Configure<ConfigurationValidationExtensions>();
```

## Verification

```bash
# 1. Run the application - should not crash
dotnet run --project src/Presentation/Platform.WebAPI

# 2. Test with missing secret - should fail
export ConnectionStrings__DefaultConnection=""
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: InvalidOperationException
# 3. Test with valid secret - should succeed
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=test;Username=test;Password=test123"
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: Application starts
```

## Priority

**IMMEDIATE** - Security critical.
