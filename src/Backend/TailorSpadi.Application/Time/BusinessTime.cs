using Microsoft.Extensions.Options;

namespace TailorSpadi.Application.Time;

public sealed class BusinessTime(IOptions<BusinessTimeOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.DefaultTimeZone);

    public DateTimeOffset ToLocalTime(DateTimeOffset instant)
    {
        return TimeZoneInfo.ConvertTime(instant, _timeZone);
    }

    public DateTime ToUtc(DateTime localTime)
    {
        if (localTime.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException("Business-local input must have an unspecified DateTime kind.", nameof(localTime));
        }

        if (_timeZone.IsInvalidTime(localTime) || _timeZone.IsAmbiguousTime(localTime))
        {
            throw new ArgumentException("The local time must identify one valid instant.", nameof(localTime));
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, _timeZone);
    }

    public (DateTime Start, DateTime End) GetUtcDayBounds(DateOnly date)
    {
        return (ToUtc(date.ToDateTime(TimeOnly.MinValue)), ToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }
}
