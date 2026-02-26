using Platform.Application.Common.Results;

namespace Platform.Application.Common.Results;

/// <summary>
/// İşlem sonucunu value olmadan temsil eder.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error     = error;
    }

    public bool  IsSuccess { get; }
    public bool  IsFailure => !IsSuccess;
    public Error Error     { get; }

    public static Result Success()              => new(true,  Error.None);
    public static Result Fail(Error error)      => new(false, error);
    public static Result Fail(string message)   => new(false, new Error(message, message));
    public static Result Fail(string code, string message) => new(false, new Error(code, message));

    // Generic helpers — allow Result.Success<T>(value) and Result.Fail<T>(...) syntax
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Fail<T>(Error error) => Result<T>.Fail(error);
    public static Result<T> Fail<T>(string code, string message) => Result<T>.Fail(code, message);
}

/// <summary>
/// İşlem sonucunu value ile birlikte temsil eder.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error error, bool success) : base(success, error)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    public static Result<T> Success(T value) => new(value, Error.None, true);
    public new static Result<T> Fail(Error error) => new(default, error, false);
    public new static Result<T> Fail(string code, string message) => new(default, new Error(code, message), false);

    public static implicit operator Result<T>(T value) => Success(value);
}
