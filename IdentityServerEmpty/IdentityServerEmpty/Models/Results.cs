

namespace IdentityServerEmpty.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error Error { get; }

    private Result(bool isSuccess, T? value, Error error)
    {
        // Guard against invalid states
        if (isSuccess && error != Error.None || !isSuccess && error != Error.None && !EqualityComparer<T?>.Default.Equals(value, FindDefault(value)))
        {
            throw new ArgumentException("Invalid error/value state", nameof(error));
        }

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    private static T? FindDefault(T? val) => default;

    // Factory methods for clean instantiation
    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);

    // Implicit operators to make returning results ultra-clean
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}