using System.Reflection;
using FluentValidation;
using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators
/// before the corresponding request handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly MethodInfo GenericFailMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m =>
            m.Name == nameof(Result.Fail) &&
            m.IsGenericMethodDefinition &&
            m.GetGenericArguments().Length == 1 &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(Error));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (failures.Length == 0)
            return await next();

        var error = new Error("VALIDATION_ERROR", string.Join(" | ", failures));
        return CreateFailureResponse(error);
    }

    private static TResponse CreateFailureResponse(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Fail(error);

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var genericFail = GenericFailMethod.MakeGenericMethod(valueType);
            var failure = genericFail.Invoke(null, [error]);

            if (failure is TResponse typedFailure)
                return typedFailure;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior cannot create a failure response for {typeof(TResponse).FullName}.");
    }
}
