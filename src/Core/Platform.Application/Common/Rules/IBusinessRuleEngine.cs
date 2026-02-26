namespace Platform.Application.Common.Rules;

/// <summary>
/// Business rule engine interface - fail-fast pattern ile kuralları sırayla çalıştırır.
/// </summary>
public interface IBusinessRuleEngine
{
    /// <summary>
    /// Kuralları sırayla çalıştırır. İlk başarısızlıkta durur (fail-fast).
    /// </summary>
    Task<Results.Result> RunAsync(CancellationToken ct, params IBusinessRule[] rules);
}
