namespace Hyperquant.Bybit.Extensions;

public static class DateTimeExtensions
{
    public static DateTime Truncate(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute,
            dateTime.Second, DateTimeKind.Utc);
    }
}