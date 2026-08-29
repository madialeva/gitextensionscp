using GitCommands;

namespace ResourceManager;

public static class LocalizationHelpers
{
    /// <summary>
    /// Takes a date/time which and determines a friendly string for time from now to be displayed for the relative time from the date.
    /// It is important to note that times are compared using the current timezone, so the date that is passed in should be converted
    /// to the local timezone before passing it in.
    /// </summary>
    /// <param name="originDate">Current date.</param>
    /// <param name="previousDate">The date to get relative time string for.</param>
    /// <param name="displayWeeks">Indicates whether to display weeks.</param>
    /// <returns>The human readable string for relative date.</returns>
    /// <see href="http://stackoverflow.com/questions/11/how-do-i-calculate-relative-time"/>
    public static string GetRelativeDateString(DateTime originDate, DateTime previousDate, bool displayWeeks = true)
    {
        RelativeDate relativeDate = GitCommands.LocalizationHelpers.GetRelativeDate(originDate, previousDate, displayWeeks);
        return relativeDate.Unit switch
        {
            RelativeDateUnit.Seconds => TranslatedStrings.GetNSecondsAgoText(relativeDate.Value),
            RelativeDateUnit.Minutes => TranslatedStrings.GetNMinutesAgoText(relativeDate.Value),
            RelativeDateUnit.Hours => TranslatedStrings.GetNHoursAgoText(relativeDate.Value),
            RelativeDateUnit.Days => TranslatedStrings.GetNDaysAgoText(relativeDate.Value),
            RelativeDateUnit.Weeks => TranslatedStrings.GetNWeeksAgoText(relativeDate.Value),
            RelativeDateUnit.Months => TranslatedStrings.GetNMonthsAgoText(relativeDate.Value),
            RelativeDateUnit.Years => TranslatedStrings.GetNYearsAgoText(relativeDate.Value),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string GetFullDateString(DateTimeOffset datetime)
    {
        return GitCommands.LocalizationHelpers.GetFullDateString(datetime);
    }
}
