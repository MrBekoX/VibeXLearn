---
name: fix-logging-security
description: Elasticsearch authentication eksik, sensitive data log masking yok, PaymentLogMasker kullanılmıyor. Log'lara password, token, card number sızabilir. PCI DSS ve GDPR uyumsuzluğu. Bu skill, log security'sini production-ready hale getirir.
---

# Fix Logging Security Issues

## Problems

**Risk Level:** HIGH

1. **Elasticsearch Authentication Missing** - Production'da credentials olmadan bağlanamaz
2. **No Sensitive Data Masking** - Password, token, card number log'a yazılabilir
3. **PaymentLogMasker Not Used** - Var ama IyzicoService'de çağrılmıyor

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Program.cs` (lines 15-20)
- `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`

## Solution Steps

### Step 1: Add Elasticsearch Authentication

Modify `src/Presentation/Platform.WebAPI/Program.cs`:

```csharp
// ── Serilog ───────────────────────────────────────────────────────────────
var elasticUrl = builder.Configuration.GetConnectionString("Elasticsearch")
    ?? "http://localhost:9200";

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()  // Add correlation ID
    .Enrich.With<SensitiveDataEnricher>();  // Custom enricher
    .WriteTo.Console();

// Add Elasticsearch with authentication
var elasticUsername = builder.Configuration["Elastic:Username"];
var elasticPassword = builder.Configuration["Elastic:Password"];

if (!string.IsNullOrEmpty(elasticUsername) && !string.IsNullOrEmpty(elasticPassword))
{
    loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
        ModifyConnectionSettings = conn =>
        {
            conn.BasicAuthentication(elasticUsername, elasticPassword);
            return conn;
        }
    });
}
else
{
    // Development: Elasticsearch without auth
    loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8
    });
}

Log.Logger = loggerConfig.CreateLogger();
```

### Step 2: Create Sensitive Data Enricher

Create file: `src/Infrastructure/Platform.Infrastructure/Logging/SensitiveDataEnricher.cs`

```csharp
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Platform.Infrastructure.Logging;

/// <summary>
/// Enricher that masks sensitive data in log events.
/// </summary>
public class SensitiveDataEnricher : ILogEventEnricher
{
    private static readonly Regex[] SensitivePatterns =
    [
        new(@"""password""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""token""\s*:\s*""[^""]{20,}""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""secretKey""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""apiKey""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""cardNumber""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""cvv""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""pan""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""iban""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""tckn""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            if (property.Value is ScalarValue scalarValue &&
                scalarValue.Value is string stringValue)
            {
                var maskedValue = MaskSensitiveData(stringValue);
                if (maskedValue != stringValue)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
                        property.Name,
                        new ScalarValue(maskedValue)));
                }
            }
        }
    }

    private static string MaskSensitiveData(string value)
    {
        foreach (var pattern in SensitivePatterns)
        {
            value = pattern.Replace(value, m =>
            {
                var key = m.Groups[1].Value;
                return $@"""{key}"": ""***""";
            });
        }
        return value;
    }
}
```

### Step 3: Update appsettings.json

Add to `src/Presentation/Platform.WebAPI/appsettings.json`:

```json
{
  "Elastic": {
    "Username": "",
    "Password": ""
  }
}
```

Add to `appsettings.Development.json`:

```json
{
  "Elastic": {
    "Username": "elastic",
    "Password": "your_dev_password"
  }
}
```

### Step 4: Use PaymentLogMasker in IyzicoService

Modify `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`:

```csharp
using System.Text.Json;

public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(...)
{
    try
    {
        // ... existing checkout form creation code ...

        var checkoutForm = await CheckoutFormInitialize.Create(request, _iyzicoOptions);

        // USE PAYMENT LOG MASKER
        var maskedResponse = PaymentLogMasker.Mask(
            JsonSerializer.Serialize(new
            {
                checkoutForm.Status,
                checkoutForm.Token,
                checkoutForm.ConversationId,
                ErrorMessage = checkoutForm.ErrorMessage
            }));

        if (checkoutForm.Status == Status.SUCCESS.ToString())
        {
            _logger.LogInformation(
                "{Event} | ConvId:{ConvId} | Response:{Response}",
                "IYZICO_CHECKOUT_INIT", conversationId, maskedResponse);

            return Result<CheckoutInitResponse>.Success(
                new CheckoutInitResponse(checkoutForm.Token!, checkoutForm.CheckoutFormContent!));
        }

        _logger.LogWarning(
            "{Event} | ConvId:{ConvId} | Response:{Response}",
            "IYZICO_CHECKOUT_FAILED", conversationId, maskedResponse);

        return Result<CheckoutInitResponse>.Fail(
            "IYZICO_INIT_FAILED", checkoutForm.ErrorMessage ?? "Iyzico initialization failed");
    }
    catch (Exception ex)
    {
        // Mask exception message too
        var maskedError = PaymentLogMasker.Mask(ex.Message);
        _logger.LogError(ex, "{Event} | ConvId:{ConvId} | Error:{Error}",
            "IYZICO_EXCEPTION", conversationId, maskedError);

        return Result<CheckoutInitResponse>.Fail(
            "IYZICO_EXCEPTION", "Payment service unavailable");
    }
}
```

### Step 5: Add Serilog Enricher Package

```xml
<!-- In Platform.Infrastructure.csproj -->
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
```

## Environment Variables for Production

```bash
# Set these in production environment
export Elastic__Username="elastic"
export Elastic__Password="your_secure_password"
```

## Verification

1. **Check logs don't contain sensitive data:**
   ```bash
   # Make a payment request
   curl -X POST http://localhost:8080/api/v1/payments/checkout ...

   # Check Elasticsearch logs
   curl -X GET "http://localhost:9200/logs-*/_search?q=token&pretty=true"
   # Should NOT show full token, only "***" or partial
   ```

2. **Test Elasticsearch auth:**
   ```bash
   # With wrong credentials - should fail
   curl -u wrong:credentials http://localhost:9200
   # Should return 401

   # With correct credentials - should succeed
   curl -u elastic:your_password http://localhost:9200
   # Should return 200
   ```

## Priority

**HIGH** - Compliance requirement (PCI DSS, GDPR).
