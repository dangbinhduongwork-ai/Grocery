using System.Globalization;

namespace GroceryHub.SharedKernel;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset VietnamNow { get; }

    Guid NewSequentialGuid();
}

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public DateTimeOffset VietnamNow => VietnamTime.ToVietnamTime(_timeProvider.GetUtcNow());

    public Guid NewSequentialGuid() => Guid.CreateVersion7(_timeProvider.GetUtcNow());
}

public static class VietnamTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static readonly TimeZoneInfo TimeZone = ResolveVietnamTimeZone();

    public static DateTimeOffset ToVietnamTime(DateTimeOffset timestamp) =>
        timestamp.ToOffset(Offset);

    public static DateOnly ToVietnamDate(DateTimeOffset timestamp)
    {
        DateTimeOffset vn = ToVietnamTime(timestamp);
        return DateOnly.FromDateTime(vn.DateTime);
    }

    public static DateTimeOffset StartOfVietnamDay(DateTimeOffset timestamp)
    {
        DateTimeOffset vn = ToVietnamTime(timestamp);
        return new DateTimeOffset(vn.Year, vn.Month, vn.Day, 0, 0, 0, Offset);
    }

    public static DateTimeOffset StartOfVietnamDayUtc(DateOnly vietnamDate) =>
        new DateTimeOffset(vietnamDate.Year, vietnamDate.Month, vietnamDate.Day, 0, 0, 0, Offset)
            .ToUniversalTime();

    public static DateTimeOffset EndOfVietnamDayExclusiveUtc(DateOnly vietnamDate) =>
        StartOfVietnamDayUtc(vietnamDate.AddDays(1));

    public static string FormatOrderDatePrefix(DateTimeOffset timestamp) =>
        ToVietnamTime(timestamp).ToString("yyMMdd", CultureInfo.InvariantCulture);

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Asia/Ho_Chi_Minh",
                    Offset,
                    "Indochina Time",
                    "Indochina Time");
            }
        }
    }
}
