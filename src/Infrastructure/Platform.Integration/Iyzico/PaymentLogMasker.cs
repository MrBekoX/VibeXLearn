using System.Text.RegularExpressions;

namespace Platform.Integration.Iyzico;

/// <summary>
/// Iyzico log masking utility.
/// </summary>
public static class PaymentLogMasker
{
    private static readonly Regex Sensitive = new(
        @"""(apiKey|secretKey|token|cardNumber|cvv|pan|iban|tckn|binNumber)""\s*:\s*""([^""]*)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Mask(string json)
        => Sensitive.Replace(json, m =>
        {
            var key   = m.Groups[1].Value;
            var value = m.Groups[2].Value;
            var masked = value.Length > 4
                ? new string('*', value.Length - 4) + value[^4..]
                : "****";
            return $@"""{key}"": ""{masked}""";
        });
}
