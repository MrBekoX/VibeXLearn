namespace Platform.Domain.Common;

/// <summary>
/// Exception thrown when domain rules are violated.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message) : base(message)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "DOMAIN_ERROR";
    }
}
