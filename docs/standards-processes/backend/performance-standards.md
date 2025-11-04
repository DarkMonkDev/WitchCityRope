# Backend Performance Standards

**Purpose**: Performance benchmarks, optimization techniques, and monitoring requirements for backend services.
**When to Read**: When implementing services, optimizing queries, or diagnosing performance issues.
**Related**: [Entity Framework Patterns](./entity-framework-patterns.md), [Service Layer Patterns](./service-layer-patterns.md)

## Performance Benchmarks and Thresholds

The following performance standards must be met for all production code:

### API Response Time Targets
- **GET endpoints**: < 200ms for simple queries, < 500ms for complex queries
- **POST/PUT endpoints**: < 1000ms for data creation/updates
- **Authentication endpoints**: < 300ms for login/logout operations
- **Search endpoints**: < 800ms for filtered/paginated results
- **File upload endpoints**: < 2000ms for files up to 10MB

### Database Query Performance
- **Simple SELECT queries**: < 50ms execution time
- **Complex JOIN queries**: < 200ms execution time
- **Bulk operations**: < 1000ms for up to 1000 records
- **Report generation**: < 5000ms for standard reports
- **Database connection pooling**: Maintain 10-50 active connections
- **Query result caching**: Cache frequently accessed data for 5-15 minutes

### Memory Usage Guidelines
- **Service memory footprint**: < 100MB per service instance at idle
- **Peak memory usage**: < 500MB per service during high load
- **Memory leak prevention**: No more than 5% memory growth per hour under load
- **Garbage collection**: < 10ms pause times for Gen 0/1, < 100ms for Gen 2
- **Object allocation**: Minimize large object heap allocations (> 85KB)

### Concurrent User Handling
- **Simultaneous users**: Support 200+ concurrent active users
- **Session management**: Handle 500+ concurrent authenticated sessions
- **Resource contention**: No deadlocks or blocking operations > 5 seconds
- **Rate limiting**: 100 requests per minute per user for API endpoints
- **Load balancing**: Scale horizontally to handle traffic spikes

### Cache Performance Standards
- **Cache hit ratio**: > 80% for frequently accessed data
- **Cache lookup time**: < 5ms for in-memory cache operations
- **Cache invalidation**: < 100ms for distributed cache updates
- **Cache storage**: Limit cache size to 20% of available memory
- **Cache expiration**: Set appropriate TTL based on data change frequency

## Performance Monitoring Implementation

```csharp
/// <summary>
/// Example of performance monitoring implementation with metrics collection
/// All services should include similar monitoring for performance tracking
/// </summary>
public class EventService : IEventService
{
    private readonly IMetrics _metrics;
    private readonly ILogger<EventService> _logger;

    public async Task<Result<EventDto>> GetEventAsync(int eventId)
    {
        using var activity = Activity.StartActivity("EventService.GetEvent");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await GetEventFromDatabaseAsync(eventId);

            // Log performance metrics
            stopwatch.Stop();
            _metrics.Counter("event_queries_total").Increment();
            _metrics.Histogram("event_query_duration_ms").Record(stopwatch.ElapsedMilliseconds);

            // Warn if query exceeds performance threshold
            if (stopwatch.ElapsedMilliseconds > 200)
            {
                _logger.LogWarning("Event query exceeded threshold: {ElapsedMs}ms for EventId {EventId}",
                    stopwatch.ElapsedMilliseconds, eventId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _metrics.Counter("event_query_errors_total").Increment();
            throw;
        }
    }
}
```

## Database Query Optimization

```csharp
/// <summary>
/// Retrieves upcoming events with optimized querying to minimize database load.
/// Uses selective loading, appropriate indexing hints, and result caching
/// to ensure responsive performance even with large event datasets.
///
/// Performance optimizations applied:
/// - Select only required columns (projection)
/// - Use indexed columns for filtering (StartTime, IsActive)
/// - Include related data in single query to avoid N+1 problems
/// - Cache results for frequently accessed data
/// - Use async methods to avoid blocking threads
/// </summary>
public async Task<List<EventSummaryDto>> GetUpcomingEventsAsync(int maxResults = 50)
{
    const string cacheKey = "upcoming_events";

    // Check cache first - upcoming events change infrequently
    if (_cache.TryGetValue<List<EventSummaryDto>>(cacheKey, out var cachedEvents))
    {
        return cachedEvents;
    }

    // Query with explicit projection to minimize data transfer
    // Only select columns needed for the summary view
    var events = await _dbContext.Events
        .Where(e => e.StartTime > DateTime.UtcNow && e.IsActive) // Use indexed columns
        .Include(e => e.Organizer) // Fetch related data in single query
        .OrderBy(e => e.StartTime) // Use indexed column for ordering
        .Take(maxResults) // Limit results to prevent large data transfers
        .Select(e => new EventSummaryDto // Project to DTO to minimize data transfer
        {
            Id = e.Id,
            Name = e.Name,
            StartTime = e.StartTime,
            Price = e.Price,
            AvailableSpots = e.Capacity - e.RegisteredCount,
            OrganizerName = e.Organizer.SceneName
        })
        .ToListAsync();

    // Cache for 10 minutes - balance between performance and data freshness
    _cache.Set(cacheKey, events, TimeSpan.FromMinutes(10));

    return events;
}
```

## Optimization Techniques

### Query Optimization
1. **Use projections**: Select only required columns with `.Select()`
2. **Avoid N+1 queries**: Use `.Include()` for related data
3. **Filter early**: Apply `.Where()` before `.Include()`
4. **Use indexed columns**: Filter and sort by indexed fields
5. **Limit results**: Always use `.Take()` for list queries

### Caching Strategies
1. **Read-heavy data**: Cache with appropriate TTL
2. **Invalidation**: Clear cache on data updates
3. **Cache keys**: Use consistent, predictable key patterns
4. **Distributed cache**: Use Redis for multi-server deployments
5. **Memory limits**: Monitor cache size and evict old entries

### Async/Await Best Practices
1. **All I/O operations**: Use async methods for database, file, network
2. **ConfigureAwait**: Use `ConfigureAwait(false)` in library code
3. **Don't block**: Never use `.Result` or `.Wait()`
4. **Cancellation tokens**: Support operation cancellation
5. **Parallel operations**: Use `Task.WhenAll()` for independent operations
