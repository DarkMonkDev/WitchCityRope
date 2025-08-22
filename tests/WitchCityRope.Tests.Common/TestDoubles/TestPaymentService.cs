using System.Collections.Concurrent;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.TestDoubles
{
    /// <summary>
    /// Test implementation of IPaymentService for unit testing
    /// </summary>
    public class TestPaymentService : IPaymentService
    {
        private readonly ConcurrentBag<ProcessedPayment> _processedPayments = new();
        private readonly ConcurrentDictionary<string, decimal> _refunds = new();

        public bool SimulateFailure { get; set; }
        public string? FailureMessage { get; set; }
        public bool SimulateProcessingDelay { get; set; }
        public int ProcessingDelayMs { get; set; } = 100;

        public IReadOnlyCollection<ProcessedPayment> ProcessedPayments => _processedPayments.ToList();

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            if (SimulateProcessingDelay)
                await Task.Delay(ProcessingDelayMs);

            if (SimulateFailure)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = FailureMessage ?? "Payment processing failed",
                    AmountCharged = 0
                };
            }

            var transactionId = $"test-txn-{Guid.NewGuid()}";
            var processedPayment = new ProcessedPayment
            {
                TransactionId = transactionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                ProcessedAt = DateTime.UtcNow,
                Metadata = request.Metadata
            };

            _processedPayments.Add(processedPayment);

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                AmountCharged = request.Amount,
                ProcessedAt = processedPayment.ProcessedAt
            };
        }

        public Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
        {
            var payment = _processedPayments.FirstOrDefault(p => p.TransactionId == transactionId);
            if (payment == null)
            {
                return Task.FromResult(new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Transaction not found"
                });
            }

            var totalRefunded = _refunds.GetOrAdd(transactionId, 0) + amount;
            if (totalRefunded > payment.Amount)
            {
                return Task.FromResult(new RefundResult
                {
                    Success = false,
                    ErrorMessage = "Refund amount exceeds original payment"
                });
            }

            _refunds[transactionId] = totalRefunded;

            return Task.FromResult(new RefundResult
            {
                Success = true,
                RefundId = $"test-refund-{Guid.NewGuid()}",
                RefundedAmount = amount
            });
        }

        public Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            var payment = _processedPayments.FirstOrDefault(p => p.TransactionId == transactionId);
            if (payment == null)
                return Task.FromResult(PaymentStatus.NotFound);

            if (_refunds.TryGetValue(transactionId, out var refundedAmount))
            {
                if (refundedAmount >= payment.Amount)
                    return Task.FromResult(PaymentStatus.Refunded);
                if (refundedAmount > 0)
                    return Task.FromResult(PaymentStatus.PartiallyRefunded);
            }

            return Task.FromResult(PaymentStatus.Completed);
        }

        public decimal GetTotalProcessed()
        {
            return _processedPayments.Sum(p => p.Amount);
        }

        public decimal GetTotalRefunded()
        {
            return _refunds.Sum(r => r.Value);
        }

        public void Clear()
        {
            _processedPayments.Clear();
            _refunds.Clear();
        }
    }

    public class ProcessedPayment
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

}