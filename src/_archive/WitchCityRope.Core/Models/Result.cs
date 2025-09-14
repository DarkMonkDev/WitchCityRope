namespace WitchCityRope.Core.Models;

/// <summary>
/// Simple result pattern for consistent error handling across all services
/// Replaces complex error handling pipelines with straightforward success/failure pattern
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;

    private Result(bool isSuccess, T? value, string error, string details)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Details = details;
    }

    /// <summary>
    /// Create successful result with value
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty, string.Empty);
    }

    /// <summary>
    /// Create failed result with error message
    /// </summary>
    public static Result<T> Failure(string error, string details = "")
    {
        return new Result<T>(false, default, error, details);
    }

    /// <summary>
    /// Implicit conversion to bool for easy success checking
    /// </summary>
    public static implicit operator bool(Result<T> result)
    {
        return result.IsSuccess;
    }
}

/// <summary>
/// Non-generic result for operations that don't return data
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;

    private Result(bool isSuccess, string error, string details)
    {
        IsSuccess = isSuccess;
        Error = error;
        Details = details;
    }

    /// <summary>
    /// Create successful result
    /// </summary>
    public static Result Success()
    {
        return new Result(true, string.Empty, string.Empty);
    }

    /// <summary>
    /// Create failed result with error message
    /// </summary>
    public static Result Failure(string error, string details = "")
    {
        return new Result(false, error, details);
    }

    /// <summary>
    /// Implicit conversion to bool for easy success checking
    /// </summary>
    public static implicit operator bool(Result result)
    {
        return result.IsSuccess;
    }
}