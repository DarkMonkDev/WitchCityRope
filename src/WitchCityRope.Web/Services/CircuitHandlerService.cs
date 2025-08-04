using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Circuit handler for monitoring Blazor Server connections
/// </summary>
public class CircuitHandlerService : CircuitHandler
{
    private readonly ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit opened: {CircuitId} at {Time}", circuit.Id, DateTime.UtcNow);
        
        // Log additional debug info in development
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Circuit details - ID: {CircuitId}, Thread: {ThreadId}", 
                circuit.Id, Thread.CurrentThread.ManagedThreadId);
        }
        
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Circuit connection down: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit connection restored: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}