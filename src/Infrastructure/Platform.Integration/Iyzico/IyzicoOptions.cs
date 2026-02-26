using System.ComponentModel.DataAnnotations;

namespace Platform.Integration.Iyzico;

/// <summary>
/// Iyzico API konfigürasyon seçenekleri.
/// </summary>
public sealed class IyzicoOptions
{
    public const string SectionName = "Iyzico";

    [Required, MinLength(10)]
    public string ApiKey { get; init; } = default!;

    [Required, MinLength(10)]
    public string SecretKey { get; init; } = default!;

    [Required, Url]
    public string BaseUrl { get; init; } = default!;

    [Required, Url]
    public string CallbackUrl { get; init; } = default!;

    [Required]
    public string Environment { get; init; } = "Sandbox";

    public bool IsProduction =>
        Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

    // Fallback values used when current user profile does not provide
    // the fields required by Iyzico checkout payload.
    public string DefaultBuyerPhone { get; init; } = "+905555555555";
    public string DefaultBuyerIdentityNumber { get; init; } = "11111111111";
    public string DefaultBuyerAddress { get; init; } = "Default Address";
    public string DefaultBuyerCity { get; init; } = "Istanbul";
    public string DefaultBuyerCountry { get; init; } = "Turkey";
    public string DefaultBuyerZipCode { get; init; } = "34000";
}
