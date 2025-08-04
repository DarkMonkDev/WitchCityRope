using AutoMapper;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Mapping
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            // Map Payment entity to ProcessPaymentResponse
            CreateMap<Payment, ProcessPaymentResponse>()
                .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.Status == Core.Enums.PaymentStatus.Completed))
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId))
                .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.Status == Core.Enums.PaymentStatus.Failed ? "Payment processing failed" : null))
                .ForMember(dest => dest.ProcessedAt, opt => opt.MapFrom(src => src.ProcessedAt));

            // Map ProcessPaymentRequest to Payment (partial mapping)
            CreateMap<ProcessPaymentRequest, Payment>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => Core.ValueObjects.Money.Create(src.Amount, "USD")))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => ParsePaymentMethod(src.PaymentMethod)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TicketId, opt => opt.Ignore())
                .ForMember(dest => dest.Ticket, opt => opt.Ignore())
                .ForMember(dest => dest.RegistrationId, opt => opt.Ignore())
                .ForMember(dest => dest.Registration, opt => opt.Ignore())
                .ForMember(dest => dest.TransactionId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ProcessedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefundAmount, opt => opt.Ignore())
                .ForMember(dest => dest.RefundedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefundTransactionId, opt => opt.Ignore())
                .ForMember(dest => dest.RefundReason, opt => opt.Ignore());
        }

        private static Core.Enums.PaymentMethod ParsePaymentMethod(string method)
        {
            return method?.ToLower() switch
            {
                "creditcard" or "credit_card" => Core.Enums.PaymentMethod.CreditCard,
                "paypal" => Core.Enums.PaymentMethod.PayPal,
                "venmo" => Core.Enums.PaymentMethod.Venmo,
                "cash" => Core.Enums.PaymentMethod.Cash,
                _ => Core.Enums.PaymentMethod.CreditCard // Default
            };
        }
    }
}