---
name: fix-iyzico-interface-duplicate
description: Application ve Integration katmanlarında aynı isimli IIyzicoService interfaceleri var, farklı signature'larla. Compile-time ambiguity ve runtime DI hatası riski. Bu skill, duplicate interface'i temizler ve tek bir contract tanımlar.
---

# Fix IIyzicoService Interface Duplicate

## Problem

**Risk Level:** MEDIUM

İki farklı `IIyzicoService` interface'i var:

1. `src/Core/Platform.Application/Common/Interfaces/IIyzicoService.cs`
   - Parametreler: `CurrentUserDto buyer, Course course`

2. `src/Infrastructure/Platform.Integration/Iyzico/IIyzicoService.cs`
   - Parametreler: `string buyerEmail, string buyerName, string buyerSurname`

Bu iki interface aynı isimde farklı signature'lara sahip. Onion Architecture'a ters:
- Interface Core'da olmalı
- Implementation Infrastructure'da olmalı

**Affected Files:**
- `src/Core/Platform.Application/Common/Interfaces/IIyzicoService.cs`
- `src/Infrastructure/Platform.Integration/Iyzico/IIyzicoService.cs`
- `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`

## Solution Steps

### Step 1: Define Single Interface in Application Layer

Update `src/Core/Platform.Application/Common/Interfaces/IIyzicoService.cs`:

```csharp
using Platform.Application.Common.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Iyzico payment gateway service contract.
/// Defined in Application layer per Onion Architecture.
/// </summary>
public interface IIyzicoService
{
    /// <summary>
    /// Initiates a checkout form for payment.
    /// </summary>
    Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
        string conversationId,
        IyzicoBuyerInfo buyer,
        string courseTitle,
        decimal price,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves checkout form result after payment callback.
    /// </summary>
    Task<Result<CheckoutResult>> RetrieveCheckoutFormAsync(
        string token,
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a payment (for refunds).
    /// </summary>
    Task<Result> CancelPaymentAsync(
        string paymentId,
        string conversationId,
        CancellationToken ct = default);
}

/// <summary>
/// Response from checkout initialization.
/// </summary>
public sealed record CheckoutInitResponse(
    string Token,
    string CheckoutFormContent);

/// <summary>
/// Result from retrieving checkout form.
/// </summary>
public sealed record CheckoutResult(
    string Status,
    string? PaymentId,
    string? ConversationId,
    string? Price,
    string? Currency,
    string? ErrorMessage);

/// <summary>
/// Buyer information for Iyzico checkout.
/// </summary>
public sealed record IyzicoBuyerInfo
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; }
    public required string IdentityNumber { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; } = "Turkey";
    public required string IpAddress { get; init; }
    public string? ZipCode { get; init; }
}
```

### Step 2: Delete Duplicate Interface

Remove: `src/Infrastructure/Platform.Integration/Iyzico/IIyzicoService.cs`

```bash
rm src/Infrastructure/Platform.Integration/Iyzico/IIyzicoService.cs
```

### Step 3: Update Implementation

Update `src/Infrastructure/Platform.Integration/Iyzico/IyzicoService.cs`:

```csharp
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Common.Interfaces;
using Platform.Integration.Iyzico;

namespace Platform.Integration.Iyzico;

/// <summary>
/// Iyzico payment gateway implementation.
/// </summary>
public sealed class IyzicoService(
    IOptions<IyzicoOptions> options,
    ILogger<IyzicoService> logger) : IIyzicoService
{
    private readonly IyzicoOptions _options = options.Value;
    private readonly ILogger<IyzicoService> _logger = logger;

    public async Task<Result<CheckoutInitResponse>> InitiateCheckoutAsync(
        string conversationId,
        IyzicoBuyerInfo buyer,
        string courseTitle,
        decimal price,
        CancellationToken ct = default)
    {
        try
        {
            var request = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = conversationId,
                Price = price.ToString("F2"),
                Currency = Currency.TRY.ToString(),
                BasketId = conversationId,
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = _options.CallbackUrl,
                Buyer = new Buyer
                {
                    Id = conversationId,
                    Name = buyer.FirstName,
                    Surname = buyer.LastName,
                    GsmNumber = buyer.Phone,
                    Email = buyer.Email,
                    IdentityNumber = buyer.IdentityNumber,
                    RegistrationAddress = buyer.Address,
                    Ip = buyer.IpAddress,
                    City = buyer.City,
                    Country = buyer.Country,
                    ZipCode = buyer.ZipCode ?? "34000"
                },
                // ... rest of the implementation
            };

            var checkoutForm = await CheckoutFormInitialize.Create(request, GetOptions());

            if (checkoutForm.Status == Status.SUCCESS.ToString())
            {
                _logger.LogInformation(
                    "{Event} | ConvId:{ConvId}",
                    "IYZICO_CHECKOUT_INIT", conversationId);

                return Result<CheckoutInitResponse>.Success(
                    new CheckoutInitResponse(
                        checkoutForm.Token!,
                        checkoutForm.CheckoutFormContent!));
            }

            _logger.LogWarning(
                "{Event} | ConvId:{ConvId} | Error:{Error}",
                "IYZICO_CHECKOUT_FAILED", conversationId, checkoutForm.ErrorMessage);

            return Result<CheckoutInitResponse>.Fail(
                "IYZICO_INIT_FAILED",
                checkoutForm.ErrorMessage ?? "Iyzico initialization failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Event} | ConvId:{ConvId}",
                "IYZICO_EXCEPTION", conversationId);

            return Result<CheckoutInitResponse>.Fail(
                "IYZICO_EXCEPTION",
                "Payment service unavailable");
        }
    }

    public async Task<Result<CheckoutResult>> RetrieveCheckoutFormAsync(
        string token,
        string conversationId,
        CancellationToken ct = default)
    {
        try
        {
            var request = new RetrieveCheckoutFormRequest
            {
                Token = token
            };

            var result = await CheckoutForm.Retrieve(request, GetOptions());

            return Result<CheckoutResult>.Success(
                new CheckoutResult(
                    result.Status ?? "unknown",
                    result.PaymentId,
                    result.ConversationId,
                    result.Price,
                    result.Currency,
                    result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{Event} | ConvId:{ConvId}",
                "IYZICO_RETRIEVE_FAILED", conversationId);

            return Result<CheckoutResult>.Fail(
                "IYZICO_RETRIEVE_FAILED",
                "Failed to retrieve payment result");
        }
    }

    public async Task<Result> CancelPaymentAsync(
        string paymentId,
        string conversationId,
        CancellationToken ct = default)
    {
        // Implementation for refund/cancel
        await Task.CompletedTask;
        return Result.Success();
    }

    private Options GetOptions() => new()
    {
        ApiKey = _options.ApiKey,
        SecretKey = _options.SecretKey,
        BaseUrl = _options.BaseUrl
    };
}
```

### Step 4: Update Command Handler

Update `InitiateCheckoutCommandHandler.cs`:

```csharp
public async Task<Result<CheckoutResponseDto>> Handle(
    InitiateCheckoutCommand request,
    CancellationToken ct)
{
    // ... business rules ...

    var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
    if (course is null)
        return Result.Fail<CheckoutResponseDto>(CourseErrors.NotFound);

    // Build buyer info
    var buyerInfo = new IyzicoBuyerInfo
    {
        Email = _currentUser.Email!,
        FirstName = _currentUser.FirstName!,
        LastName = _currentUser.LastName!,
        Phone = request.BuyerPhone,
        IdentityNumber = request.BuyerIdentityNumber,
        Address = request.BuyerAddress ?? "Default Address",
        City = request.BuyerCity ?? "İstanbul",
        IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0"
    };

    var checkoutResult = await _iyzicoService.InitiateCheckoutAsync(
        conversationId,
        buyerInfo,
        course.Title,
        course.Price,
        ct);

    // ... rest of handler ...
}
```

### Step 5: Register in DI

Update `src/Infrastructure/Platform.Integration/Extensions/IntegrationServiceExtensions.cs`:

```csharp
// Register with Application layer interface
services.AddScoped<IIyzicoService, IyzicoService>();
```

## Verification

```bash
# Build solution - should have no errors
dotnet build

# Test payment checkout
curl -X POST http://localhost:8080/api/v1/payments/checkout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": "...",
    "buyerPhone": "+905551234567",
    "buyerIdentityNumber": "12345678901",
    "buyerAddress": "Test Address",
    "buyerCity": "İstanbul"
  }'

# Should return checkout form content
```

## Priority

**SHORT-TERM** - Architecture consistency.
