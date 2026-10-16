using System.Globalization;

namespace CivicConnect.Web.Models;

/// <summary>
/// How a time reads on screen. Times are stored in UTC; pages show them in South
/// African Standard Time, the clock CivicConnect's users go by, for example
/// "14 Oct 2026, 16:31". The datetime attribute on each time element keeps the
/// exact UTC instant for machines. CSV exports stay in UTC and say so.
/// </summary>
public static class DisplayTime
{
    private const string Format = "d MMM yyyy, HH:mm";

    // The IANA name works on Windows and Linux. South Africa has no daylight
    // saving, so a fixed UTC+2 is an exact fallback if the name is unknown.
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.TryFindSystemTimeZoneById("Africa/Johannesburg", out var zone)
            ? zone
            : TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "South Africa Standard Time", "SAST");

    public static string Show(DateTimeOffset value)
        => TimeZoneInfo.ConvertTime(value, Zone).ToString(Format, CultureInfo.InvariantCulture);
}
