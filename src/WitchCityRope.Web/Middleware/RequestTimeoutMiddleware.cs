namespace WitchCityRope.Web.Middleware;

/// <summary>
/// Middleware to handle request timeouts and provide better error responses
/// </summary>
public class RequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimeoutMiddleware> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public RequestTimeoutMiddleware(RequestDelegate next, ILogger<RequestTimeoutMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var cts = new CancellationTokenSource(_defaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, context.RequestAborted);

        var originalToken = context.RequestAborted;
        context.RequestAborted = linkedCts.Token;

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Request timeout for {Path} after {Timeout}ms", 
                context.Request.Path, _defaultTimeout.TotalMilliseconds);

            context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
            
            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                await HandleAdminTimeout(context);
            }
            else
            {
                await context.Response.WriteAsync("Request timeout. Please try again.");
            }
        }
        catch (OperationCanceledException)
        {
            // Client cancelled the request
            _logger.LogInformation("Request cancelled by client for {Path}", context.Request.Path);
            throw;
        }
        finally
        {
            context.RequestAborted = originalToken;
        }
    }

    private async Task HandleAdminTimeout(HttpContext context)
    {
        context.Response.ContentType = "text/html";
        
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Request Timeout</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 600px;
            margin: 50px auto;
            padding: 20px;
            text-align: center;
        }
        .error-icon {
            font-size: 48px;
            color: #ff6b6b;
            margin-bottom: 20px;
        }
        h1 {
            color: #333;
            margin-bottom: 10px;
        }
        p {
            color: #666;
            margin-bottom: 20px;
        }
        .actions {
            margin-top: 30px;
        }
        .btn {
            display: inline-block;
            padding: 10px 20px;
            margin: 0 10px;
            background-color: #6366f1;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            transition: background-color 0.3s;
        }
        .btn:hover {
            background-color: #4f46e5;
        }
        .btn-secondary {
            background-color: #6b7280;
        }
        .btn-secondary:hover {
            background-color: #4b5563;
        }
    </style>
</head>
<body>
    <div class='error-icon'>⏱️</div>
    <h1>Request Timeout</h1>
    <p>The admin page is taking longer than expected to load. This might be due to heavy server load or network issues.</p>
    <p>Please try one of the following:</p>
    <div class='actions'>
        <a href='javascript:location.reload()' class='btn'>Retry</a>
        <a href='/admin/dashboard' class='btn btn-secondary'>Go to Dashboard</a>
    </div>
</body>
</html>";

        await context.Response.WriteAsync(html);
    }
}

/// <summary>
/// Extension method for adding the timeout middleware
/// </summary>
public static class RequestTimeoutMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTimeout(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimeoutMiddleware>();
    }
}