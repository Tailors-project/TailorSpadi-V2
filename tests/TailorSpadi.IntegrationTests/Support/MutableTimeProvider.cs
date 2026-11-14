namespace TailorSpadi.IntegrationTests.Support;

/// <summary>Real time plus an adjustable offset, so tests can age tokens without sleeping.</summary>
public sealed class MutableTimeProvider : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() =>
        DateTimeOffset.UtcNow + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));

    public void Advance(TimeSpan amount) => Interlocked.Add(ref _offsetTicks, amount.Ticks);

    public void Reset() => Interlocked.Exchange(ref _offsetTicks, 0);
}
