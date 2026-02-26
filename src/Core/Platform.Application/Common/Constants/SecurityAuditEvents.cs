namespace Platform.Application.Common.Constants;

/// <summary>
/// Security audit log event tipleri.
/// </summary>
public static class SecurityAuditEvents
{
    public const string LoginSuccess        = "SEC_LOGIN_OK";
    public const string LoginFailed         = "SEC_LOGIN_FAIL";
    public const string TokenRefreshed      = "SEC_TOKEN_REFRESH";
    public const string TokenRevoked        = "SEC_TOKEN_REVOKE";
    public const string PasswordChanged     = "SEC_PWD_CHANGE";
    public const string UnauthorizedAccess  = "SEC_UNAUTHORIZED";
    public const string PaymentAttempt      = "SEC_PAYMENT_ATTEMPT";
    public const string PaymentSuccess      = "SEC_PAYMENT_OK";
    public const string PaymentFailed       = "SEC_PAYMENT_FAIL";
}
