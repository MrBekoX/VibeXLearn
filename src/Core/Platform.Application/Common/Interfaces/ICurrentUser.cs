namespace Platform.Application.Common.Interfaces;

/// <summary>
/// Mevcut kullanıcı bilgilerine erişim interface'i.
/// </summary>
public interface ICurrentUser
{
    Guid    UserId    { get; }
    string? Email     { get; }
    bool    IsAuthenticated { get; }
    bool    IsInRole(string role);
    IEnumerable<string> Roles { get; }
}
