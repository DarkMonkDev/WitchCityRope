using System;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for payment service operations (API layer)
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Processes a payment request
        /// </summary>
        Task<ProcessPaymentResponse> ProcessPaymentAsync(Core.DTOs.ProcessPaymentRequest request, Guid userId);

        /// <summary>
        /// Handles webhook callbacks from payment provider
        /// </summary>
        Task HandleWebhookAsync(string json, string signature, string webhookSecret);
    }
}