using WitchCityRope.Api.Features.Health.Models;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test builder for HealthResponse following Vertical Slice Architecture patterns
/// Used for testing health check responses and API contract validation
/// </summary>
public class HealthResponseBuilder : TestDataBuilder<HealthResponse, HealthResponseBuilder>
{
    private string _status;
    private DateTime _timestamp;
    private bool _databaseConnected;
    private int _userCount;
    private string _version;

    public HealthResponseBuilder()
    {
        // Set default valid values for health response
        _status = "Healthy";
        _timestamp = DateTime.UtcNow;
        _databaseConnected = true;
        _userCount = _faker.Random.Int(1, 100);
        _version = "1.0.0";
    }

    public HealthResponseBuilder WithStatus(string status)
    {
        _status = status;
        return This;
    }

    public HealthResponseBuilder Healthy()
    {
        _status = "Healthy";
        _databaseConnected = true;
        return This;
    }

    public HealthResponseBuilder Unhealthy()
    {
        _status = "Unhealthy";
        _databaseConnected = false;
        return This;
    }

    public HealthResponseBuilder Degraded()
    {
        _status = "Degraded";
        _databaseConnected = true;
        return This;
    }

    public HealthResponseBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return This;
    }

    public HealthResponseBuilder WithCurrentTimestamp()
    {
        _timestamp = DateTime.UtcNow;
        return This;
    }

    public HealthResponseBuilder WithDatabaseConnected(bool connected)
    {
        _databaseConnected = connected;
        return This;
    }

    public HealthResponseBuilder WithDatabaseDisconnected()
    {
        _databaseConnected = false;
        return This;
    }

    public HealthResponseBuilder WithUserCount(int userCount)
    {
        _userCount = userCount;
        return This;
    }

    public HealthResponseBuilder WithNoUsers()
    {
        _userCount = 0;
        return This;
    }

    public HealthResponseBuilder WithManyUsers()
    {
        _userCount = _faker.Random.Int(1000, 10000);
        return This;
    }

    public HealthResponseBuilder WithVersion(string version)
    {
        _version = version;
        return This;
    }

    public override HealthResponse Build()
    {
        return new HealthResponse
        {
            Status = _status,
            Timestamp = _timestamp,
            DatabaseConnected = _databaseConnected,
            UserCount = _userCount,
            Version = _version
        };
    }
}

/// <summary>
/// Test builder for DetailedHealthResponse with additional metrics
/// </summary>
public class DetailedHealthResponseBuilder : TestDataBuilder<DetailedHealthResponse, DetailedHealthResponseBuilder>
{
    private string _status;
    private DateTime _timestamp;
    private bool _databaseConnected;
    private int _userCount;
    private string _version;
    private string _databaseVersion;
    private int _activeUserCount;
    private string _environment;

    public DetailedHealthResponseBuilder()
    {
        // Set default valid values for detailed health response
        _status = "Healthy";
        _timestamp = DateTime.UtcNow;
        _databaseConnected = true;
        _userCount = _faker.Random.Int(1, 100);
        _version = "1.0.0";
        _databaseVersion = "PostgreSQL 16.1";
        _activeUserCount = _faker.Random.Int(0, _userCount);
        _environment = "Development";
    }

    public DetailedHealthResponseBuilder WithStatus(string status)
    {
        _status = status;
        return This;
    }

    public DetailedHealthResponseBuilder Healthy()
    {
        _status = "Healthy";
        _databaseConnected = true;
        return This;
    }

    public DetailedHealthResponseBuilder Unhealthy()
    {
        _status = "Unhealthy";
        _databaseConnected = false;
        return This;
    }

    public DetailedHealthResponseBuilder WithDatabaseVersion(string databaseVersion)
    {
        _databaseVersion = databaseVersion;
        return This;
    }

    public DetailedHealthResponseBuilder WithActiveUserCount(int activeUserCount)
    {
        _activeUserCount = Math.Min(activeUserCount, _userCount);
        return This;
    }

    public DetailedHealthResponseBuilder WithEnvironment(string environment)
    {
        _environment = environment;
        return This;
    }

    public DetailedHealthResponseBuilder InProduction()
    {
        _environment = "Production";
        return This;
    }

    public DetailedHealthResponseBuilder InDevelopment()
    {
        _environment = "Development";
        return This;
    }

    public DetailedHealthResponseBuilder InStaging()
    {
        _environment = "Staging";
        return This;
    }

    public override DetailedHealthResponse Build()
    {
        return new DetailedHealthResponse
        {
            Status = _status,
            Timestamp = _timestamp,
            DatabaseConnected = _databaseConnected,
            UserCount = _userCount,
            Version = _version,
            DatabaseVersion = _databaseVersion,
            ActiveUserCount = _activeUserCount,
            Environment = _environment
        };
    }
}