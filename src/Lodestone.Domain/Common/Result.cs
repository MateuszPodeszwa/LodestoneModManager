namespace Lodestone.Domain.Common;

/// <summary>
/// Railway-oriented result. Expected failures are returned as values rather than thrown, so the
/// application layer composes operations without exception-driven control flow. See
/// <see cref="Result{T}"/> for the value-carrying variant.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(new Error(code, message));
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, Error.None) => _value = value;

    private Result(Error error)
        : base(false, error) => _value = default;

    /// <summary>The success value. Throws if the result is a failure — guard with <see cref="Result.IsSuccess"/>.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error}).");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);

    public static new Result<T> Failure(string code, string message) => new(new Error(code, message));

    /// <summary>Lets a bare value flow into a successful result (railway ergonomics).</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Collapses both branches into a single output.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Error);

    /// <summary>Transforms the success value, propagating failure untouched.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
        => IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(Error);

    /// <summary>Chains another result-producing operation, short-circuiting on failure.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
        => IsSuccess ? bind(_value!) : Result<TOut>.Failure(Error);
}
