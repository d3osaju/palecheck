namespace PaleCheck.Core;

/// <summary>What happens to 1,000 people under a screening strategy.</summary>
public record ScreeningOutcome(int Anaemic, int Found, int Missed, int FalseAlarms, int BloodTests)
{
    public double TestsPerCaseFound => Found == 0 ? double.PositiveInfinity : (double)BloodTests / Found;
}

/// <summary>Screening arithmetic for the "should a photo decide who gets tested?" calculator.</summary>
public static class Screening
{
    /// <summary>Phone check first; only people it flags get a blood test.</summary>
    public static ScreeningOutcome PhoneFirst(double prevalence, double sensitivity, double specificity)
    {
        int anaemic = (int)Math.Round(1000 * prevalence);
        int healthy = 1000 - anaemic;
        int found = (int)Math.Round(anaemic * sensitivity);
        int falseAlarms = (int)Math.Round(healthy * (1 - specificity));
        return new ScreeningOutcome(anaemic, found, anaemic - found, falseAlarms, found + falseAlarms);
    }

    /// <summary>Everyone gets a blood test (what Anaemia Mukt Bharat does in schools).</summary>
    public static ScreeningOutcome TestEveryone(double prevalence)
    {
        int anaemic = (int)Math.Round(1000 * prevalence);
        return new ScreeningOutcome(anaemic, anaemic, 0, 0, 1000);
    }
}
