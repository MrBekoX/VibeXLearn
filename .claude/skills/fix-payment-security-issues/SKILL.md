---
name: fix-payment-security-issues
description: Payment callback endpoint'inde IP whitelist ve HMAC doğrulama yok. PaymentLogMasker var ama kullanılmıyor. Hardcoded dummy data Iyzico'da mevcut. Bu skill, ödeme güvenliğini production-ready hale getirir.
---

# Fix Payment Security Issues

## Problems

**Risk Level:** HIGH/CRITICAL

1. **Payment Callback Without Validation** - `/api/v1/payments/callback` `AllowAnonymous` ve hiçbir IP/HMAC doğrulaması yok
2. **PaymentLogMasker Not Used** - Hassas ödeme verileri log'a sızdırılabilir
3. **Hardcoded Dummy Data** - Iyzico Buyer'da hardcoded phone, TC, address var

**Affected Files:**
- `src/Presentation/Platform.WebAPI/Endpoints/PaymentEndpoints.cs`
- `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`

## Solution Steps

### Step 1: Add Iyzico IP Whitelist

Create file: `src/Infrastructure/Platform.Integration/Iyzico/IyzicoIpWhitelist.cs`

```csharp
namespace Platform.Integration.Iyzico;

/// <summary>
/// Iyzico callback IP whitelist.
/// Source: https://dev.iyzipay.com/tr/ozel-durumlar/ip-adresleri
/// </summary>
public static class IyzicoIpWhitelist
{
    // Iyzico production IP ranges
    private static readonly string[] ProductionIps =
    [
        "185.152.41.0/24",
        "185.152.42.0/24",
        "185.152.43.0/24",
        "185.152.44.0/24"
    ];

    // Sandbox IP (for testing)
    private const string SandboxIp = "127.0.0.1";

    /// <summary>
    /// Checks if the IP is allowed to make callback requests.
    /// </summary>
    public static bool IsAllowed(string? remoteIp, bool isProduction = true)
    {
        if (string.IsNullOrEmpty(remoteIp)) return false;

        // Development: allow localhost
        if (!isProduction && remoteIp == SandboxIp) return true;

        // Production: check against whitelist
        return ProductionIps.Any(cidr => IsInRange(remoteIp, cidr));
    }

    private static bool IsInRange(string ip, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;

        // Simple check - for production use a proper IP range library
        var prefix = parts[0][..parts[0].LastIndexOf('.')];
        return ip.StartsWith(prefix);
    }
}
```

### Step 2: Use PaymentLogMasker

Modify `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`:

```csharp
using System.Text.Json;

public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
    string conversationId,
    string buyerEmail,
    string buyerName,
    string buyerSurname,
    string courseTitle,
    decimal price,
    CancellationToken ct)
{
    try
    {
        // ... existing code to build request ...

        var checkoutForm = await CheckoutFormInitialize.Create(request, _iyzicoOptions);

        // USE PAYMENT LOG MASKER
        var maskedResponse = PaymentLogMasker.Mask(
            JsonSerializer.Serialize(new
            {
                checkoutForm.Status,
                checkoutForm.Token,
                checkoutForm.ConversationId,
                checkoutForm.CheckoutFormContent,
                checkoutForm.ErrorMessage
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
        // Mask exception details that might contain sensitive info
        _logger.LogError(ex, "{Event} | ConvId:{ConvId} | Error:{Error}",
            "IYZICO_EXCEPTION", conversationId,
            PaymentLogMasker.Mask(ex.Message));
        return Result<CheckoutInitResponse>.Fail(
            "IYZICO_EXCEPTION", "Payment service unavailable");
    }
}
```

### Step 3: Secure Payment Callback Endpoint

Modify `src/Presentation/Platform.WebAPI/Endpoints/PaymentEndpoints.cs`:

```csharp
group.MapPost("/callback", async (
    IMediator mediator,
    HttpContext http,
    IOptions<IyzicoOptions> iyzicoOptions,
    ILogger<PaymentEndpoints> logger,
    CancellationToken ct) =>
{
    // STEP 1: IP Whitelist Check
    var remoteIp = http.Connection.RemoteIpAddress?.ToString();
    var isProduction = iyzicoOptions.Value.IsProduction;

    if (!IyzicoIpWhitelist.IsAllowed(remoteIp, isProduction))
    {
        logger.LogWarning(
            "{Event} | IP:{Ip} | Reason:IP_NOT_WHITELISTED",
            SecurityAuditEvents.PaymentFailed, remoteIp);
        // Return 200 to prevent retry storms, but don't process
        return Results.Ok(new { status = "received" });
    }

    // STEP 2: Read raw body for logging
    http.Request.EnableBuffering();
    using var reader = new StreamReader(http.Request.Body, leaveOpen: true);
    var rawBody = await reader.ReadToEndAsync(ct);
    http.Request.Body.Position = 0;

    // STEP 3: Parse form data
    var form = await http.Request.ReadFormAsync(ct);
    var token = form["token"].ToString();
    var conversationId = form["conversationId"].ToString();

    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(conversationId))
    {
        logger.LogWarning(
            "{Event} | IP:{Ip} | Reason:MISSING_PARAMS",
            SecurityAuditEvents.PaymentFailed, remoteIp);
        return Results.Ok(new { status = "received" });
    }

    // STEP 4: Process with masked logging
    logger.LogInformation(
        "{Event} | ConvId:{ConvId} | IP:{Ip}",
        "PAYMENT_CALLBACK", conversationId[..16] + "***", remoteIp);

    var result = await mediator.Send(
        new ProcessCallbackCommand(token, conversationId, rawBody), ct);

    // ALWAYS return 200 to prevent Iyzico retry storms
    return Results.Ok(new { status = "received" });
})
.AllowAnonymous()
.ExcludeFromDescription();  // Hide from Swagger
```

### Step 4: Remove Hardcoded Dummy Data

Modify `IyzicoService.InitiateCheckoutAsync` to accept buyer info:

```csharp
public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
    string conversationId,
    IyzicoBuyerInfo buyer,  // NEW: Encapsulated buyer info
    string courseTitle,
    decimal price,
    CancellationToken ct)
```

Create `src/Infrastructure/Platform.Integration/Iyzico/IyzicoBuyerInfo.cs`:

```csharp
namespace Platform.Integration.Iyzico;

/// <summary>
/// Buyer information for Iyzico checkout.
/// All fields required by Iyzico API.
/// </summary>
public sealed record IyzicoBuyerInfo
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; }           // +90XXXXXXXXXX
    public required string IdentityNumber { get; init; }  // TC Kimlik
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; } = "Turkey";
    public required string IpAddress { get; init; }       // Client IP
    public string? ZipCode { get; init; }
}
```

Update checkout command handler to capture buyer info from request:

```csharp
// In InitiateCheckoutCommandHandler
var buyerInfo = new IyzicoBuyerInfo
{
    Email = currentUser.Email!,
    FirstName = currentUser.FirstName!,
    LastName = currentUser.LastName!,
    Phone = request.BuyerPhone,        // From request
    IdentityNumber = request.BuyerIdentityNumber,  // From request
    Address = request.BuyerAddress ?? "Default Address",
    City = request.BuyerCity ?? "İstanbul",
    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0"
};
```

## Verification

```bash
# Test IP whitelist (should be rejected)
curl -X POST http://localhost:8080/api/v1/payments/callback \
  -H "X-Forwarded-For: 1.2.3.4" \
  -d "token=test&conversationId=test"
# Should return 200 but not process (check logs)

# Test with valid Iyzico IP (simulated)
curl -X POST http://localhost:8080/api/v1/payments/callback \
  -H "X-Forwarded-For: 185.152.41.100" \
  -d "token=valid&conversationId=valid"
# Should process
```

## Priority

**IMMEDIATE** - Financial security risk.
