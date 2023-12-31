﻿namespace TokiwadaiPride.Bot;

internal static class DateTimeExtensions
{
    public static DateTime LastMondayMidnight(this DateTime dateTime)
    {
        var dayStart = dateTime.Date;
        int offset = dayStart.DayOfWeek == DayOfWeek.Sunday ? -7 : -(int)dayStart.DayOfWeek;
        return dayStart.AddDays(offset + (int)DayOfWeek.Monday);
    }

    public static DateTime NextSundayMidnight(this DateTime dateTime)
    {
        return LastMondayMidnight(dateTime).AddDays(7).AddTicks(-1);
    }

    public static DateTime BeginningOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Date.Year, dateTime.Date.Month, 1);
    }

    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return BeginningOfMonth(dateTime).AddMonths(1).AddTicks(-1);
    }
}
