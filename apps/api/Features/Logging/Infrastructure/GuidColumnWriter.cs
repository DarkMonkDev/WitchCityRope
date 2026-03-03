using NpgsqlTypes;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace WitchCityRope.Api.Features.Logging.Infrastructure;

/// <summary>
/// Column writer that extracts a named property and writes it as a UUID.
/// Handles both Guid values (from LogContext.PushProperty) and string values
/// (from message template parameters like {UserId}) by parsing strings to Guid.
/// This prevents InvalidCastException when code logs {UserId} as a string
/// but the PostgreSQL column expects UUID.
/// </summary>
public class GuidColumnWriter : ColumnWriterBase
{
    private readonly string _propertyName;

    public GuidColumnWriter(string propertyName)
        : base(NpgsqlDbType.Uuid)
    {
        _propertyName = propertyName;
    }

    public override object? GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null)
    {
        if (!logEvent.Properties.TryGetValue(_propertyName, out var propertyValue))
            return DBNull.Value;

        if (propertyValue is ScalarValue scalar)
        {
            if (scalar.Value is Guid guid)
                return guid;

            if (scalar.Value is string str && Guid.TryParse(str, out var parsed))
                return parsed;
        }

        return DBNull.Value;
    }
}
