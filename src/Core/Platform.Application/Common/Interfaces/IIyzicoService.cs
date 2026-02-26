using Platform.Application.Common.Results;
using Platform.Domain.Entities;

namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Application-level contract for Iyzico payment operations.
/// Implemented by Platform.Integration.
/// </summary>
public interface IIyzicoService
{
    /// <summary>
    /// Initializes a hosted checkout form. Returns token + form HTML.
    /// </summary>
    Task<Result<CheckoutInitResult>> InitiateCheckoutAsync(
        string       conversationId,
        CurrentUserDto buyer,
        Course       course,
        decimal      amount,
        CancellationToken ct);

    /// <summary>
    /// Queries Iyzico to verify the checkout result by token.
    /// Returns null if the token cannot be resolved.
    /// </summary>
    Task<RetrieveCheckoutFormResult?> RetrieveCheckoutFormAsync(
        string token,
        string conversationId,
        CancellationToken ct);
}

/// <summary>Iyzico checkout init success payload.</summary>
public sealed record CheckoutInitResult(string Token, string CheckoutFormContent);

/// <summary>Iyzico checkout retrieve result payload.</summary>
public sealed record RetrieveCheckoutFormResult(
    string? Status,
    string? PaymentId,
    string? ConversationId,
    string? Price,
    string? Currency);
