using System;

namespace WitchCityRope.Tests.Common.Models
{
    /// <summary>
    /// Simple Result pattern implementation for TDD tests
    /// This will be replaced by proper Result pattern from Core during implementation
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public string ErrorMessage { get; protected set; } = string.Empty;
        public Exception? Exception { get; protected set; }

        protected Result(bool isSuccess, string errorMessage = "", Exception? exception = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result Success() => new(true);

        public static Result Failure(string errorMessage) => new(false, errorMessage);

        public static Result Failure(Exception exception) => new(false, exception.Message, exception);

        public static Result<T> Success<T>(T value) => new(value, true);

        public static Result<T> Failure<T>(string errorMessage) => new(default, false, errorMessage);

        public static Result<T> Failure<T>(Exception exception) => new(default, false, exception.Message, exception);
    }

    /// <summary>
    /// Generic Result pattern for TDD tests
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        internal Result(T? value, bool isSuccess, string errorMessage = "", Exception? exception = null)
            : base(isSuccess, errorMessage, exception)
        {
            Value = value;
        }
    }
}