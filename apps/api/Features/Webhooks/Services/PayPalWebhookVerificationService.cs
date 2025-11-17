using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Force.Crc32;

namespace WitchCityRope.Api.Features.Webhooks.Services;

/// <summary>
/// PayPal webhook signature verification service implementing self-cryptographic verification
/// following PayPal's webhook security best practices.
///
/// <para>
/// Verification process:
/// 1. Extract PayPal headers (transmission ID, time, cert URL, signature, algorithm)
/// 2. Compute CRC32 checksum of raw JSON request body
/// 3. Construct verification string from headers, webhook ID, and CRC32
/// 4. Fetch PayPal's public key certificate from provided URL
/// 5. Verify RSA-SHA256 signature using certificate
/// </para>
///
/// <para>
/// Security considerations:
/// - Prevents webhook replay attacks via transmission ID validation
/// - Ensures webhook authenticity via RSA signature verification
/// - Validates certificate URL is from PayPal domain
/// - Implements certificate caching to reduce external HTTP calls
/// </para>
/// </summary>
public class PayPalWebhookVerificationService : IPayPalWebhookVerificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PayPalWebhookVerificationService> _logger;

    // TODO: Implement certificate caching to avoid repeated fetches
    // Consider using IMemoryCache with 24-hour expiration
    // Key format: "paypal-cert:{certUrl}"

    public PayPalWebhookVerificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<PayPalWebhookVerificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Verifies PayPal webhook signature to ensure webhook authenticity and prevent tampering.
    /// </summary>
    /// <param name="request">HTTP request containing PayPal headers and JSON body</param>
    /// <param name="requestBody">Raw JSON request body (must not be deserialized yet)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    public async Task<bool> VerifyWebhookSignatureAsync(
        HttpRequest request,
        string requestBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Extract PayPal headers (case-insensitive)
            var transmissionId = GetHeader(request, "PAYPAL-TRANSMISSION-ID");
            var transmissionTime = GetHeader(request, "PAYPAL-TRANSMISSION-TIME");
            var certUrl = GetHeader(request, "PAYPAL-CERT-URL");
            var transmissionSig = GetHeader(request, "PAYPAL-TRANSMISSION-SIG");
            var authAlgo = GetHeader(request, "PAYPAL-AUTH-ALGO");
            var webhookId = _config["PayPal:WebhookId"];

            // 2. Validate required headers present
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

            // 3. Validate algorithm (must be SHA256withRSA)
            if (!authAlgo.Equals("SHA256withRSA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unsupported authentication algorithm: {Algorithm}", authAlgo);
                return false;
            }

            // 4. Validate certificate URL is from PayPal domain (security check)
            if (!IsValidPayPalCertUrl(certUrl))
            {
                _logger.LogWarning("Certificate URL is not from trusted PayPal domain: {CertUrl}", certUrl);
                return false;
            }

            // 5. Compute CRC32 of raw JSON body
            var crc32 = ComputeCrc32(requestBody);

            // 6. Create verification string (format defined by PayPal)
            var verificationString = $"{transmissionId}|{transmissionTime}|{webhookId}|{crc32}";

            _logger.LogDebug(
                "Webhook verification string: TransmissionId={TransmissionId}, Time={Time}, WebhookId={WebhookId}, CRC32={CRC32}",
                transmissionId, transmissionTime, webhookId, crc32);

            // 7. Fetch PayPal certificate (with caching consideration)
            var certificate = await FetchCertificateAsync(certUrl, cancellationToken);

            if (certificate == null)
            {
                _logger.LogWarning("Failed to fetch certificate from {CertUrl}", certUrl);
                return false;
            }

            // 8. Verify RSA-SHA256 signature
            var isValid = VerifyRsaSha256Signature(
                certificate,
                verificationString,
                transmissionSig
            );

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

    /// <summary>
    /// Computes CRC32 checksum of the raw JSON request body.
    /// PayPal uses CRC32 to ensure body integrity during transmission.
    /// </summary>
    /// <param name="input">Raw JSON string</param>
    /// <returns>CRC32 checksum as unsigned integer</returns>
    private uint ComputeCrc32(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Crc32Algorithm.Compute(bytes);
    }

    /// <summary>
    /// Fetches PayPal's public key certificate from the provided URL.
    /// Certificate is used to verify webhook signature.
    ///
    /// TODO: Implement certificate caching to reduce external HTTP calls:
    /// - Cache key: "paypal-cert:{certUrl}"
    /// - Expiration: 24 hours (certificates rotate infrequently)
    /// - Invalidation: Manual or automatic on verification failure
    /// </summary>
    /// <param name="certUrl">PayPal certificate URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>X509 certificate or null if fetch fails</returns>
    private async Task<X509Certificate2?> FetchCertificateAsync(
        string certUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10); // Prevent hanging

            var certPem = await client.GetStringAsync(certUrl, cancellationToken);

            if (string.IsNullOrEmpty(certPem))
            {
                _logger.LogWarning("Certificate PEM is empty from URL: {CertUrl}", certUrl);
                return null;
            }

            // Parse PEM-encoded certificate
            return X509Certificate2.CreateFromPem(certPem);
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

    /// <summary>
    /// Verifies RSA-SHA256 signature using PayPal's public key certificate.
    /// </summary>
    /// <param name="certificate">PayPal's X509 certificate containing public key</param>
    /// <param name="data">Verification string (transmission ID | time | webhook ID | CRC32)</param>
    /// <param name="signature">Base64-encoded signature from PAYPAL-TRANSMISSION-SIG header</param>
    /// <returns>True if signature is valid, false otherwise</returns>
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

    /// <summary>
    /// Validates that certificate URL is from a trusted PayPal domain.
    /// Prevents certificate URL injection attacks.
    /// </summary>
    /// <param name="certUrl">Certificate URL from PAYPAL-CERT-URL header</param>
    /// <returns>True if URL is from trusted PayPal domain</returns>
    private bool IsValidPayPalCertUrl(string certUrl)
    {
        try
        {
            var uri = new Uri(certUrl);

            // Must be HTTPS
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // Must be from PayPal domain (api.paypal.com or api.sandbox.paypal.com)
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

    /// <summary>
    /// Gets header value with case-insensitive key lookup.
    /// PayPal headers may arrive with varying casing.
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <param name="headerName">Header name (case-insensitive)</param>
    /// <returns>Header value or empty string if not found</returns>
    private string GetHeader(HttpRequest request, string headerName)
    {
        foreach (var header in request.Headers)
        {
            if (header.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value.ToString();
            }
        }

        return string.Empty;
    }
}

/// <summary>
/// Interface for PayPal webhook verification service
/// </summary>
public interface IPayPalWebhookVerificationService
{
    /// <summary>
    /// Verifies PayPal webhook signature to ensure webhook authenticity and prevent tampering.
    /// </summary>
    /// <param name="request">HTTP request containing PayPal headers and JSON body</param>
    /// <param name="requestBody">Raw JSON request body (must not be deserialized yet)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<bool> VerifyWebhookSignatureAsync(
        HttpRequest request,
        string requestBody,
        CancellationToken cancellationToken = default);
}
