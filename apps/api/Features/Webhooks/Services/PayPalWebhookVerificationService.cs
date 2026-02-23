using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Force.Crc32;
using Microsoft.Extensions.Caching.Memory;

namespace WitchCityRope.Api.Features.Webhooks.Services;

/// <summary>
/// PayPal webhook signature verification service implementing self-cryptographic verification.
/// Uses certificate caching to prevent repeated HTTP fetches and DoS via webhook spam.
/// </summary>
public class PayPalWebhookVerificationService : IPayPalWebhookVerificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PayPalWebhookVerificationService> _logger;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CertificateCacheDuration = TimeSpan.FromHours(24);

    public PayPalWebhookVerificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<PayPalWebhookVerificationService> logger,
        IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> VerifyWebhookSignatureAsync(
        HttpRequest request,
        string requestBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transmissionId = GetHeader(request, "PAYPAL-TRANSMISSION-ID");
            var transmissionTime = GetHeader(request, "PAYPAL-TRANSMISSION-TIME");
            var certUrl = GetHeader(request, "PAYPAL-CERT-URL");
            var transmissionSig = GetHeader(request, "PAYPAL-TRANSMISSION-SIG");
            var authAlgo = GetHeader(request, "PAYPAL-AUTH-ALGO");
            var webhookId = _config["PayPal:WebhookId"];

            if (string.IsNullOrEmpty(transmissionId))
            {
                _logger.LogWarning("Missing PAYPAL-TRANSMISSION-ID header");
                return false;
            }

            if (string.IsNullOrEmpty(transmissionTime))
            {
                _logger.LogWarning("Missing PAYPAL-TRANSMISSION-TIME header");
                return false;
            }

            if (string.IsNullOrEmpty(certUrl))
            {
                _logger.LogWarning("Missing PAYPAL-CERT-URL header");
                return false;
            }

            if (string.IsNullOrEmpty(transmissionSig))
            {
                _logger.LogWarning("Missing PAYPAL-TRANSMISSION-SIG header");
                return false;
            }

            if (string.IsNullOrEmpty(authAlgo))
            {
                _logger.LogWarning("Missing PAYPAL-AUTH-ALGO header");
                return false;
            }

            if (string.IsNullOrEmpty(webhookId))
            {
                _logger.LogWarning("PayPal:WebhookId not configured in application settings");
                return false;
            }

            if (!authAlgo.Equals("SHA256withRSA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unsupported authentication algorithm: {Algorithm}", authAlgo);
                return false;
            }

            if (!IsValidPayPalCertUrl(certUrl))
            {
                _logger.LogWarning("Certificate URL is not from trusted PayPal domain: {CertUrl}", certUrl);
                return false;
            }

            var crc32 = ComputeCrc32(requestBody);
            var verificationString = $"{transmissionId}|{transmissionTime}|{webhookId}|{crc32}";

            _logger.LogDebug(
                "Webhook verification string: TransmissionId={TransmissionId}, Time={Time}, WebhookId={WebhookId}, CRC32={CRC32}",
                transmissionId, transmissionTime, webhookId, crc32);

            var certificate = await FetchCertificateAsync(certUrl, cancellationToken);

            if (certificate == null)
            {
                _logger.LogWarning("Failed to fetch certificate from {CertUrl}", certUrl);
                return false;
            }

            var isValid = VerifyRsaSha256Signature(certificate, verificationString, transmissionSig);

            if (isValid)
            {
                _logger.LogInformation(
                    "Webhook signature verified successfully for transmission ID {TransmissionId}",
                    transmissionId);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook signature verification failed for transmission ID {TransmissionId}",
                    transmissionId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during webhook verification");
            return false;
        }
    }

    private uint ComputeCrc32(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Crc32Algorithm.Compute(bytes);
    }

    /// <summary>
    /// Fetches PayPal's certificate with 24-hour caching to prevent repeated HTTP calls.
    /// </summary>
    private async Task<X509Certificate2?> FetchCertificateAsync(
        string certUrl,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"paypal-cert:{certUrl}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out byte[]? cachedCertBytes) && cachedCertBytes != null)
        {
            _logger.LogDebug("Using cached PayPal certificate for {CertUrl}", certUrl);
            try
            {
                return new X509Certificate2(cachedCertBytes);
            }
            catch (CryptographicException)
            {
                // Cache was corrupted, remove and re-fetch
                _cache.Remove(cacheKey);
            }
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var certPem = await client.GetStringAsync(certUrl, cancellationToken);

            if (string.IsNullOrEmpty(certPem))
            {
                _logger.LogWarning("Certificate PEM is empty from URL: {CertUrl}", certUrl);
                return null;
            }

            var certificate = X509Certificate2.CreateFromPem(certPem);

            // Cache the raw certificate data
            _cache.Set(cacheKey, certificate.RawData, CertificateCacheDuration);
            _logger.LogDebug("Cached PayPal certificate for {CertUrl} (expires in 24h)", certUrl);

            return certificate;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching certificate from {CertUrl}", certUrl);
            return null;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error parsing certificate from {CertUrl}", certUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching certificate from {CertUrl}", certUrl);
            return null;
        }
    }

    private bool VerifyRsaSha256Signature(
        X509Certificate2 certificate,
        string data,
        string signature)
    {
        try
        {
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogWarning("Certificate does not contain RSA public key");
                return false;
            }

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 signature format");
            return false;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Cryptographic error during signature verification");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during signature verification");
            return false;
        }
    }

    private bool IsValidPayPalCertUrl(string certUrl)
    {
        try
        {
            var uri = new Uri(certUrl);

            if (uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var validHosts = new[]
            {
                "api.paypal.com",
                "api.sandbox.paypal.com",
                "api-m.paypal.com",
                "api-m.sandbox.paypal.com"
            };

            return validHosts.Any(host =>
                uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private string GetHeader(HttpRequest request, string headerName)
    {
        foreach (var header in request.Headers)
        {
            if (header.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                return header.Value.ToString();
        }
        return string.Empty;
    }
}

/// <summary>
/// Interface for PayPal webhook verification service.
/// </summary>
public interface IPayPalWebhookVerificationService
{
    Task<bool> VerifyWebhookSignatureAsync(
        HttpRequest request,
        string requestBody,
        CancellationToken cancellationToken = default);
}
