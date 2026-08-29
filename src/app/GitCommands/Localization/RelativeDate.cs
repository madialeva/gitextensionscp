namespace GitCommands;

/// <summary>Defines the unit used by a relative date.</summary>
public enum RelativeDateUnit
{
    Seconds,
    Minutes,
    Hours,
    Days,
    Weeks,
    Months,
    Years
}

/// <summary>Represents the signed value and unit of a relative date.</summary>
public readonly record struct RelativeDate(RelativeDateUnit Unit, int Value);
