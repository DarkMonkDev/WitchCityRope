using System;

namespace WitchCityRope.Core
{
    /// <summary>
    /// Base exception for domain-specific business rule violations
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException()
        {
        }

        public DomainException(string message)
            : base(message)
        {
        }

        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an entity is not found
    /// </summary>
    public class EntityNotFoundException : DomainException
    {
        public string EntityName { get; }
        public object EntityId { get; }

        public EntityNotFoundException(string entityName, object entityId)
            : base($"{entityName} with id '{entityId}' was not found.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        public EntityNotFoundException(string entityName, object entityId, Exception innerException)
            : base($"{entityName} with id '{entityId}' was not found.", innerException)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }

    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    public class BusinessRuleViolationException : DomainException
    {
        public string RuleName { get; }

        public BusinessRuleViolationException(string ruleName, string message)
            : base(message)
        {
            RuleName = ruleName;
        }

        public BusinessRuleViolationException(string ruleName, string message, Exception innerException)
            : base(message, innerException)
        {
            RuleName = ruleName;
        }
    }

    /// <summary>
    /// Exception thrown when there's insufficient capacity
    /// </summary>
    public class InsufficientCapacityException : DomainException
    {
        public int RequestedCapacity { get; }
        public int AvailableCapacity { get; }

        public InsufficientCapacityException(int requested, int available)
            : base($"Insufficient capacity. Requested: {requested}, Available: {available}")
        {
            RequestedCapacity = requested;
            AvailableCapacity = available;
        }
    }

    /// <summary>
    /// Exception thrown when a payment operation fails
    /// </summary>
    public class PaymentException : DomainException
    {
        public string TransactionId { get; }
        public string PaymentMethod { get; }

        public PaymentException(string message)
            : base(message)
        {
        }

        public PaymentException(string message, string transactionId, string paymentMethod)
            : base(message)
        {
            TransactionId = transactionId;
            PaymentMethod = paymentMethod;
        }

        public PaymentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an operation is not authorized
    /// </summary>
    public class UnauthorizedOperationException : DomainException
    {
        public string Operation { get; }
        public string UserId { get; }

        public UnauthorizedOperationException(string operation, string userId)
            : base($"User '{userId}' is not authorized to perform operation '{operation}'")
        {
            Operation = operation;
            UserId = userId;
        }
    }
}