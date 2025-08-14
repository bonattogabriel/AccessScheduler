namespace AccessScheduler.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ConvertToUtc(this DateTime dateTime, string timeZoneId)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZoneInfo);
    }

    public static DateTime ConvertFromUtc(this DateTime utcDateTime, string timeZoneId)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
    }

    public static DateTime MaxDateTime(DateTime date1, DateTime date2)
    {
        return date1 > date2 ? date1 : date2;
    }

    public static DateTime MinDateTime(DateTime date1, DateTime date2)
    {
        return date1 < date2 ? date1 : date2;
    }

    public static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
