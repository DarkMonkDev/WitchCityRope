using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Tests.Common.Builders
{
    public class PaymentBuilder : TestDataBuilder<Payment, PaymentBuilder>
    {
        private Registration _registration;
        private Money _amount;
        private string _paymentMethod;
        private string _transactionId;
        private PaymentStatus _status = PaymentStatus.Pending;

        public PaymentBuilder()
        {
            // Set default valid values
            _registration = new RegistrationBuilder().Build();
            _amount = _registration.SelectedPrice;
            _paymentMethod = "Stripe";
            _transactionId = $"txn_{_faker.Random.AlphaNumeric(16)}";
        }

        public PaymentBuilder WithRegistration(Registration registration)
        {
            _registration = registration;
            _amount = registration.SelectedPrice; // Default to registration's selected price
            return This;
        }

        public PaymentBuilder WithAmount(Money amount)
        {
            _amount = amount;
            return This;
        }

        public PaymentBuilder WithAmount(decimal amount)
        {
            _amount = Money.Create(amount);
            return This;
        }

        public PaymentBuilder WithAmount(decimal amount, string currency)
        {
            _amount = Money.Create(amount, currency);
            return This;
        }

        public PaymentBuilder WithPaymentMethod(string paymentMethod)
        {
            _paymentMethod = paymentMethod;
            return This;
        }

        public PaymentBuilder WithTransactionId(string transactionId)
        {
            _transactionId = transactionId;
            return This;
        }

        public PaymentBuilder AsCompleted()
        {
            _status = PaymentStatus.Completed;
            return This;
        }

        public PaymentBuilder AsFailed()
        {
            _status = PaymentStatus.Failed;
            return This;
        }

        public PaymentBuilder AsRefunded()
        {
            _status = PaymentStatus.Refunded;
            return This;
        }

        public override Payment Build()
        {
            var payment = new Payment(
                _registration,
                _amount,
                _paymentMethod,
                _transactionId
            );

            // Apply status if not pending
            switch (_status)
            {
                case PaymentStatus.Completed:
                    payment.MarkAsCompleted();
                    break;
                case PaymentStatus.Failed:
                    payment.MarkAsFailed();
                    break;
                case PaymentStatus.Refunded:
                    payment.MarkAsCompleted(); // Must be completed first
                    payment.InitiateRefund();
                    break;
            }

            return payment;
        }
    }
}