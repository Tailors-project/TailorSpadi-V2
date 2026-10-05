using System.Text.Json;
using Microsoft.Extensions.Options;
using TailorSpadi.Application.Time;

namespace TailorSpadi.UnitTests;

public sealed class BusinessTimeTests
{
    private readonly BusinessTime _businessTime = new(Options.Create(new BusinessTimeOptions()));

    [Fact]
    public void UtcInstantCrossesMidnightInLagos()
    {
        var instant = new DateTimeOffset(2026, 9, 27, 23, 30, 0, TimeSpan.Zero);
        DateTimeOffset local = _businessTime.ToLocalTime(instant);

        Assert.Equal(new DateTime(2026, 9, 28, 0, 30, 0), local.DateTime);
        Assert.Equal(TimeSpan.FromHours(1), local.Offset);
        Assert.Equal(instant, local);
    }

    [Fact]
    public void BusinessAppointmentConvertsToUtc()
    {
        DateTime utc = _businessTime.ToUtc(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void DayBoundsUseBusinessMidnight()
    {
        (DateTime start, DateTime end) = _businessTime.GetUtcDayBounds(new DateOnly(2026, 9, 28));

        Assert.Equal(new DateTime(2026, 9, 27, 23, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 9, 28, 23, 0, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public void DateOnlyJsonPreservesSelectedDate()
    {
        var date = new DateOnly(2026, 9, 28);
        Assert.Equal("\"2026-09-28\"", JsonSerializer.Serialize(date));
        Assert.Equal(date, JsonSerializer.Deserialize<DateOnly>("\"2026-09-28\""));
    }

    [Fact]
    public void LocalInputMustNotBeFalselyMarkedAsUtc()
    {
        Assert.Throws<ArgumentException>(() =>
            _businessTime.ToUtc(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc)));
    }
}
