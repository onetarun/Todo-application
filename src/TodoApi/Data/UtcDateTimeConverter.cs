using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TodoApi.Data;

// datetime2 has no time zone, so SQL Server hands values back as DateTimeKind.Unspecified and they would be
// serialized without a "Z". All DateTimes in this app are UTC: store them as UTC and read them back marked as UTC.
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => ToUtc(v),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
{
    // A value without an offset is taken to be UTC already; a local value is converted.
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };
}
