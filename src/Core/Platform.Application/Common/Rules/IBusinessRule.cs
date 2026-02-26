using Platform.Application.Common.Results;

namespace Platform.Application.Common.Rules;

/// <summary>
/// İş kuralı kontratı. Her kural bir Code, Message ve asenkron CheckAsync metoduna sahiptir.
/// </summary>
public interface IBusinessRule
{
    string Code    { get; }
    string Message { get; }
    Task<Result> CheckAsync(CancellationToken ct);
}
