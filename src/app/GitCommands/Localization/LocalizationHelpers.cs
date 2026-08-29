namespace GitCommands;

/// <summary>Provides portable date calculations used by presentation layers.</summary>
public static class LocalizationHelpers
{
    private static DateTime RoundDateTime(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }

    /// <summary>
    /// Determines the signed unit and value for the relative time between two dates.
    /// </summary>
    /// <param name="originDate">Current date.</param>
    /// <param name="previousDate">The date to calculate relative time for.</param>
    /// <param name="displayWeeks">Indicates whether to display weeks.</param>
    /// <returns>The unit and signed value for the relative date.</returns>
    public static RelativeDate GetRelativeDate(DateTime originDate, DateTime previousDate, bool displayWeeks = true)
    {
        TimeSpan timeSpan = new(RoundDateTime(originDate).Ticks - RoundDateTime(previousDate).Ticks);
        double delta = Math.Abs(timeSpan.TotalSeconds);

        if (delta < 60)
        {
            return new(RelativeDateUnit.Seconds, timeSpan.Seconds);
        }

        if (delta < 45 * 60)
        {
            return new(RelativeDateUnit.Minutes, timeSpan.Minutes);
        }

        if (delta < 24 * 60 * 60)
        {
            int hours = delta < 60 * 60 ? Math.Sign(timeSpan.Minutes) * 1 : timeSpan.Hours;
            return new(RelativeDateUnit.Hours, hours);
        }

        if (delta < (displayWeeks ? 7 : 30) * 24 * 60 * 60)
        {
            return new(RelativeDateUnit.Days, timeSpan.Days);
        }

        if (displayWeeks && delta < 30 * 24 * 60 * 60)
        {
            int weeks = Convert.ToInt32(timeSpan.Days / 7.0);
            return new(RelativeDateUnit.Weeks, weeks);
        }

        if (delta < 365 * 24 * 60 * 60)
        {
            int months = Convert.ToInt32(timeSpan.Days / 30.0);
            return new(RelativeDateUnit.Months, months);
        }

        int years = Convert.ToInt32(timeSpan.Days / 365.0);
        return new(RelativeDateUnit.Years, years);
    }

    /// <summary>Formats a date using the current culture's general local date pattern.</summary>
    /// <param name="dateTime">Date to format.</param>
    /// <returns>The formatted local date.</returns>
    public static string GetFullDateString(DateTimeOffset dateTime)
    {
        return dateTime.LocalDateTime.ToString("G");
    }
}
