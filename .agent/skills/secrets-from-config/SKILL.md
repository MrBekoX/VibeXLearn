---
name: secrets-from-config
description: Move secrets from appsettings.json to environment variables or user-secrets. appsettings.json'da plaintext secrets are security risk. Use environment variables in production.
---

# Remove Secrets from Configuration Files

## Problem

**Risk Level:** CRITICAL

Connection strings, API keys, and sensitive data in `appsettings.json` files are a significant security risk:
- Credentials can be leaked via source control
- Production deployments with placeholder values will fail
- Compliance violations (OWASP A02)

**Affected Files:**
- `src/Presentation/Platform.WebAPI/appsettings.json`
- `src/Presentation/Platform.WebAPI/appsettings.Development.json`
- `src/Presentation/Platform.WebAPI/appsettings.Production.json`

## Solution Steps

### Step 1: Remove Secrets from appsettings.json

Update `appsettings.json` to remove actual secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "Redis": "",
    "Elasticsearch": ""
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
    "DefaultConnection": "Host=localhost;Port=5432;Database=vibexlearn;Username=vibex_user;Password=dev_password;Pooling=true;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    // For development only - DO NOT commit
    "Secret": "dev_secret_key_minimum_32_chars_long"
  }
}
```

### Step 3: Create appsettings.Production.json (Empty Template)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "",
    "Redis": "",
    "Elasticsearch": ""
  }
}
```
**NOTE:** In production, all secrets MUST come from environment variables or a secrets vault.

### Step 4: Add to .gitignore

```gitignore
# Secrets
.env
appsettings.*.local.json
*.pfx
*.key
```

### Step 5: Use User Secrets for Development
```bash
cd src/Presentation/Platform.WebAPI

# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=vibexlearn;Username=vibex_user;Password=your_password"
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
dotnet user-secrets set "Jwt:Secret" "your_development_secret_minimum_32_characters"
dotnet user-secrets set "Iyzico:ApiKey" "your_iyzico_api_key"
dotnet user-secrets set "Iyzico:SecretKey" "your_iyzico_secret_key"
```

### Step 6: Use Environment Variables for Production

```bash
# In production, set these environment variables
export ConnectionStrings__DefaultConnection="Host=prod-host;Port=5432;Database=prod_db;Username=prod_user;Password=prod_password"
export ConnectionStrings__Redis="prod-redis:6379,password=prod_redis_password"
export ConnectionStrings__Elasticsearch="http://elasticsearch:9200"
export Jwt__Secret="your_production_secret_minimum_32_characters"
export Iyzico__ApiKey="your_production_api_key"
export Iyzico__SecretKey="your_production_secret_key"
```

### Step 7: Update Docker Compose for Secrets

Update `docker/compose/docker-compose.yml`:

```yaml
services:
  api:
    environment:
      ConnectionStrings__DefaultConnection: ${ConnectionStrings__DefaultConnection}
      ConnectionStrings__Redis: ${ConnectionStrings__Redis}
      ConnectionStrings__Elasticsearch: ${ConnectionStrings__Elasticsearch}
      Jwt__Secret: ${Jwt__Secret}
      Iyzico__ApiKey: ${Iyzico__ApiKey}
      Iyzico__SecretKey: ${Iyzico__SecretKey}
```

### Step 8: Add Validation on Startup

Create `src/Presentation/Platform.WebAPI/Extensions/ConfigurationValidationExtensions.cs`

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
        ValidateRequired(config, "Iyzico:ApiKey", env);
        ValidateRequired(config, "Iyzico:SecretKey", env);
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

### Step 9: Register in Program.cs

```csharp
builder.Services.Configure<ConfigurationValidationExtensions>();
```

### Step 10: Add Vault Integration (Optional, for Production)

```csharp
// For Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    builder.Configuration["KeyVault:Uri"],
    new DefaultKeyChainSecretClient());

// For AWS Secrets Manager
builder.AddSecretsManager(
    builder.Configuration["Aws:SecretsManager:Name"]);
```

## Verification
```bash
# 1. Check that appsettings.json doesn't contain secrets
grep -i "password\|secret\|key" src/Presentation/Platform.WebAPI/appsettings.json
# Should return nothing

# 2. Run application with missing secrets (should fail)
export ConnectionStrings__DefaultConnection=""
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: InvalidOperationException

# 3. Run with valid secrets
export ConnectionStrings__DefaultConnection="valid_connection_string"
dotnet run --project src/Presentation/Platform.WebAPI
# Expected: Application starts
```

## Priority
**IMMEDIATE** - Security critical.
