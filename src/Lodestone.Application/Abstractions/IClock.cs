namespace Lodestone.Application.Abstractions;

/// <summary>Abstraction over the system clock so time-dependent logic is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Default clock backed by <see cref="DateTimeOffset.UtcNow"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
