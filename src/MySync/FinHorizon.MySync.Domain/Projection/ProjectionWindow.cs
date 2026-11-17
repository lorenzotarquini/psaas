namespace FinHorizon.MySync.Domain.Projection;

/// <summary>
/// UC14-R3: the months the suggested parameters average over. The last <see cref="MaxMonths"/> calendar months before the
/// current one, keeping only those that started on or after the opening of the first account; installments use the last
/// <see cref="MaxInstallmentMonths"/> of them. The current month is incomplete and never counts.
/// </summary>
public sealed record ProjectionWindow(DateOnly From, DateOnly To, int Months, DateOnly InstallmentsFrom, int InstallmentMonths)
{
    public const int MaxMonths = 6;

    public const int MaxInstallmentMonths = 3;

    /// <summary>No account yet, or none older than a whole month: nothing to average.</summary>
    public bool IsEmpty => Months == 0;

    public static ProjectionWindow For(DateOnly today, DateOnly? firstOpening)
    {
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var to = currentMonth.AddDays(-1);
        var months = 0;
        if (firstOpening is { } opening)
        {
            while (months < MaxMonths && currentMonth.AddMonths(-(months + 1)) >= opening)
            {
                months++;
            }
        }

        var installmentMonths = Math.Min(MaxInstallmentMonths, months);
        return new ProjectionWindow(
            currentMonth.AddMonths(-months),
            to,
            months,
            currentMonth.AddMonths(-installmentMonths),
            installmentMonths);
    }

    /// <summary>The monthly average of <paramref name="total"/> over <paramref name="months"/>, in cents; 0 without months.</summary>
    public static decimal Average(decimal total, int months) =>
        months == 0 ? 0m : decimal.Round(total / months, 2, MidpointRounding.AwayFromZero);
}
