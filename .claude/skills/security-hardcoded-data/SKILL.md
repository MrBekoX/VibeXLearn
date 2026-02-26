---
name: security-hardcoded-data
description: Remove hardcoded sensitive data from code and move to secure configuration with validation.
---

# Security Hardcoded Data Fix

Remove hardcoded sensitive data from source code and implement secure configuration patterns.

## Problem

```csharp
// ❌ BAD: Hardcoded sensitive data
public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(...)
{
    var request = new CreateCheckoutFormInitializeRequest
    {
        Buyer = new Buyer
        {
            GsmNumber = "+905350000000",           // Hardcoded!
            IdentityNumber = "74300864791",         // Hardcoded! (Real TC!)
            RegistrationAddress = "İstanbul...",    // Hardcoded!
            Ip = "85.34.78.112",                    // Hardcoded!
            // ...
        }
    };
}
```

## Solutions

### Solution 1: Configuration Options Pattern

```csharp
// Configuration class
public class IyzicoOptions
{
    public const string SectionName = "Iyzico";

    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public bool IsProduction { get; set; } = false;
    
    // Default buyer settings
    public IyzicoBuyerDefaults BuyerDefaults { get; set; } = new();
}

public class IyzicoBuyerDefaults
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
```

```json
// appsettings.json
{
  "Iyzico": {
    "Environment": "Sandbox",
    "BaseUrl": "https://sandbox-api.iyzipay.com",
    "CallbackUrl": "https://api.example.com/payments/callback",
    "BuyerDefaults": {
      "PhoneNumber": "+905001234567",
      "IdentityNumber": "12345678901",
      "Address": "Default Address, Istanbul",
      "City": "Istanbul",
      "Country": "Turkey"
    }
  }
}
```

### Solution 2: Request-Based Dynamic Data

```csharp
// DTO with all required fields
public record PaymentInitiationRequest(
    string CourseId,
    string BuyerEmail,
    string BuyerName,
    string BuyerSurname,
    string BuyerPhone,          // Required
    string BuyerIdentityNumber, // Required
    string IpAddress            // Captured from request
);

// Service implementation
public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
    PaymentInitiationRequest request,
    CancellationToken ct)
{
    // Validate required fields
    if (string.IsNullOrWhiteSpace(request.BuyerPhone))
        return Result.Fail<CheckoutInitResponse>("PAYMENT_PHONE_REQUIRED", "Phone number is required");
    
    if (string.IsNullOrWhiteSpace(request.BuyerIdentityNumber))
        return Result.Fail<CheckoutInitResponse>("PAYMENT_IDENTITY_REQUIRED", "Identity number is required");

    var iyzicoRequest = new CreateCheckoutFormInitializeRequest
    {
        Buyer = new Buyer
        {
            Id = request.CourseId,
            Name = request.BuyerName,
            Surname = request.BuyerSurname,
            Email = request.BuyerEmail,
            GsmNumber = request.BuyerPhone,
            IdentityNumber = request.BuyerIdentityNumber,
            Ip = request.IpAddress,
            RegistrationAddress = _options.BuyerDefaults.Address,
            City = _options.BuyerDefaults.City,
            Country = _options.BuyerDefaults.Country
        }
    };
}
```

### Solution 3: IP Address Extraction

```csharp
// Extension method
public static class HttpContextExtensions
{
    public static string GetClientIpAddress(this HttpContext context)
    {
        // Check forwarded headers (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take first IP if multiple
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check CF-Connecting-IP (Cloudflare)
        var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp))
            return cfIp;

        // Fallback to connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}

// Usage in endpoint
app.MapPost("/api/payments/checkout", async (
    PaymentInitiationDto dto,
    HttpContext httpContext,
    IIyzicoService service,
    CancellationToken ct) =>
{
    var ipAddress = httpContext.GetClientIpAddress();
    
    var request = new PaymentInitiationRequest(
        dto.CourseId,
        dto.BuyerEmail,
        dto.BuyerName,
        dto.BuyerSurname,
        dto.BuyerPhone,
        dto.BuyerIdentityNumber,
        ipAddress
    );

    var result = await service.InitiateCheckoutAsync(request, ct);
    // ...
});
```

### Solution 4: Secure Configuration with Validation

```csharp
// Program.cs
builder.Services.AddOptions<IyzicoOptions>()
    .Bind(builder.Configuration.GetSection(IyzicoOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(opt =>
    {
        // Prevent sandbox in production
        if (opt.IsProduction &&
            opt.BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CRITICAL: Sandbox Iyzico URL cannot be used in Production.");
        }

        // Enforce HTTPS in production
        if (opt.IsProduction && !opt.CallbackUrl.StartsWith("https://"))
        {
            throw new InvalidOperationException(
                "CRITICAL: Iyzico CallbackUrl must use HTTPS in Production.");
        }

        // Validate no placeholder values
        if (opt.BuyerDefaults.IdentityNumber == "74300864791" ||
            opt.BuyerDefaults.IdentityNumber == "12345678901")
        {
            throw new InvalidOperationException(
                "CRITICAL: Default identity number must be changed from placeholder.");
        }

        return true;
    }, "Validation failed")
    .ValidateOnStart();
```

## Best Practices

1. **Never commit secrets**: Use `.gitignore` for config files with secrets
2. **Use environment variables** in production
3. **Validate on startup**: Catch misconfiguration early
4. **Use placeholder detection**: Prevent default values in production
5. **Rotate credentials regularly**: Have a process for key rotation
6. **Use secret management**: Azure Key Vault, AWS Secrets Manager, etc.
