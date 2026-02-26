using Platform.Application.Common.Results;

namespace Platform.Application.Common.Rules;

/// <summary>
/// Business rule engine implementasyonu - fail-fast pattern.
/// </summary>
public sealed class BusinessRuleEngine : IBusinessRuleEngine
{
    public async Task<Result> RunAsync(CancellationToken ct, params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            var result = await rule.CheckAsync(ct);
            if (result.IsFailure)
                return result; // fail-fast: ilk başarısızlıkta dur
        }
        return Result.Success();
    }
}
