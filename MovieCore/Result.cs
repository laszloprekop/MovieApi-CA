namespace MovieCore;

public enum ErrorType
{
    None,
    NotFound,
    BusinessRule
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, ErrorType errorType, string? error)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Error = error;
    }

    public static Result Success() => new(true, ErrorType.None, null);

    public static Result NotFound(string? error = null) => new(false, ErrorType.NotFound, error);

    public static Result BusinessRule(string? error = null) => new(false, ErrorType.BusinessRule, error);

    public static Result<T> Success<T>(T value) => new(value, true, ErrorType.None, null);
    public static Result<T> NotFound<T>(string error) => new(default!, false, ErrorType.NotFound, error);
    public static Result<T> BusinessRule<T>(string error) => new(default!, false, ErrorType.BusinessRule, error);
}

public sealed class Result<T> : Result
{
    public T Value { get; }

    internal Result(T value, bool isSuccess, ErrorType errorType, string? error) : base(isSuccess, errorType, error) =>
        Value = value;
}