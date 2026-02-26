namespace Platform.Integration.Iyzico;

/// <summary>
/// Iyzico callback IP whitelist for security.
/// SKILL: fix-payment-security-issues
/// Source: https://dev.iyzipay.com/tr/ozel-durumlar/ip-adresleri
/// </summary>
public static class IyzicoIpWhitelist
{
    // Iyzico production IP ranges (CIDR notation)
    private static readonly string[] ProductionRanges =
    [
        "185.152.41.0/24",
        "185.152.42.0/24",
        "185.152.43.0/24",
        "185.152.44.0/24"
    ];

    /// <summary>
    /// Checks if the IP is allowed to make callback requests.
    /// </summary>
    /// <param name="remoteIp">The remote IP address</param>
    /// <param name="isProduction">Whether running in production mode</param>
    /// <returns>True if IP is allowed</returns>
    public static bool IsAllowed(string? remoteIp, bool isProduction = true)
    {
        if (string.IsNullOrEmpty(remoteIp))
            return false;

        // Remove port if present
        var ip = remoteIp.Split(':').First();

        // Development mode: allow localhost and private networks
        if (!isProduction)
        {
            if (IsLocalhost(ip))
                return true;

            // Allow Docker network in development (172.x.x.x)
            if (ip.StartsWith("172."))
                return true;

            // Allow private networks
            if (ip.StartsWith("10.") || ip.StartsWith("192.168."))
                return true;
        }

        // Production: check against Iyzico IP whitelist
        return ProductionRanges.Any(range => IsInCidrRange(ip, range));
    }

    private static bool IsLocalhost(string ip)
    {
        return ip == "127.0.0.1" || ip == "::1" || ip == "localhost";
    }

    private static bool IsInCidrRange(string ip, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2) return false;

            var baseAddress = parts[0];
            var prefixLength = int.Parse(parts[1]);

            var ipBytes = System.Net.IPAddress.Parse(ip).GetAddressBytes();
            var baseBytes = System.Net.IPAddress.Parse(baseAddress).GetAddressBytes();

            if (ipBytes.Length != baseBytes.Length) return false;

            var maskBytes = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            // Check complete bytes
            for (var i = 0; i < maskBytes; i++)
            {
                if (ipBytes[i] != baseBytes[i])
                    return false;
            }

            // Check remaining bits
            if (remainingBits > 0 && maskBytes < ipBytes.Length)
            {
                var mask = (byte)(0xFF << (8 - remainingBits));
                if ((ipBytes[maskBytes] & mask) != (baseBytes[maskBytes] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            // Fallback: simple prefix check for /24
            var prefix = cidr.Split('/')[0];
            var prefixParts = prefix.Split('.');
            var ipParts = ip.Split('.');

            if (prefixParts.Length != 4 || ipParts.Length != 4) return false;

            // Check first 3 octets for /24
            return prefixParts[0] == ipParts[0] &&
                   prefixParts[1] == ipParts[1] &&
                   prefixParts[2] == ipParts[2];
        }
    }
}
