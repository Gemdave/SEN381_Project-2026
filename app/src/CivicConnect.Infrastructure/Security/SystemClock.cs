using CivicConnect.Application.Abstractions;

namespace CivicConnect.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
