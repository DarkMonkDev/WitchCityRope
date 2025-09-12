using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<WitchCityRope.Web.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WitchCityRopeDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<WitchCityRopeDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Override problematic services with test doubles
            services.AddScoped<IEmailService, TestEmailService>();
            services.AddScoped<IPaymentService, TestPaymentService>();
            
            // Remove all health check registrations to avoid duplicates
            var healthCheckBuilders = services.Where(
                d => d.ServiceType == typeof(Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder)).ToList();
            foreach (var builder in healthCheckBuilders)
            {
                services.Remove(builder);
            }
            
            // Remove health check services
            var healthCheckServices = services.Where(
                d => d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("HealthCheck")).ToList();
            foreach (var service in healthCheckServices)
            {
                services.Remove(service);
            }

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
                db.Database.EnsureCreated();
            }
        });

        builder.UseEnvironment("Testing");
        
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }
}

// Test doubles
public class TestEmailService : IEmailService
{
    public Task<bool> SendEmailAsync(Core.ValueObjects.EmailAddress to, string subject, string body, bool isHtml = true)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendBulkEmailAsync(IEnumerable<Core.ValueObjects.EmailAddress> to, string subject, string body, bool isHtml = true)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendTemplateEmailAsync(Core.ValueObjects.EmailAddress to, string templateName, object templateData)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendRegistrationConfirmationAsync(Core.ValueObjects.EmailAddress to, string sceneName, string eventTitle, DateTime eventDate)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendCancellationConfirmationAsync(Core.ValueObjects.EmailAddress to, string sceneName, string eventTitle, Core.ValueObjects.Money? refundAmount = null)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendVettingStatusUpdateAsync(Core.ValueObjects.EmailAddress to, string sceneName, string status, string? notes = null)
    {
        return Task.FromResult(true);
    }
}

public class TestPaymentService : IPaymentService
{
    public Task<PaymentResult> ProcessPaymentAsync(Core.Entities.Registration registration, Core.ValueObjects.Money amount, string paymentMethodId)
    {
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = "TEST_" + Guid.NewGuid().ToString(),
            Status = Core.Enums.PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        });
    }

    public Task<RefundResult> ProcessRefundAsync(Core.Entities.Payment payment, Core.ValueObjects.Money? refundAmount = null, string? reason = null)
    {
        return Task.FromResult(new RefundResult
        {
            Success = true,
            RefundTransactionId = "REFUND_" + Guid.NewGuid().ToString(),
            RefundedAmount = refundAmount ?? payment.Amount,
            ProcessedAt = DateTime.UtcNow
        });
    }

    public Task<bool> ValidatePaymentMethodAsync(string paymentMethodId)
    {
        return Task.FromResult(true);
    }

    public Task<PaymentIntent> CreatePaymentIntentAsync(Core.ValueObjects.Money amount, PaymentMetadata metadata)
    {
        return Task.FromResult(new PaymentIntent
        {
            ClientSecret = "test_secret_" + Guid.NewGuid().ToString(),
            IntentId = "test_intent_" + Guid.NewGuid().ToString(),
            Amount = amount,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
    }

    public Task<Core.Enums.PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        return Task.FromResult(Core.Enums.PaymentStatus.Completed);
    }
}