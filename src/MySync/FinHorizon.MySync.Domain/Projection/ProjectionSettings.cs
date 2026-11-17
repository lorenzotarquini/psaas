using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Projection;

/// <summary>UC14: a one-off income (positive) or expense (negative) in month <see cref="Month"/> of the projection.</summary>
public sealed class ProjectionEvent
{
    public ProjectionEvent(int month, decimal amount, string note)
    {
        Month = month;
        Amount = amount;
        Note = note;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private ProjectionEvent()
    {
        Note = null!;
    }

    public int Month { get; private set; }

    public decimal Amount { get; private set; }

    public string Note { get; private set; }
}

/// <summary>UC14: the monthly payment the user expects for one debt.</summary>
public sealed class ProjectionPayment
{
    public ProjectionPayment(Guid debtId, decimal amount)
    {
        DebtId = debtId;
        Amount = amount;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private ProjectionPayment()
    {
    }

    public Guid DebtId { get; private set; }

    public decimal Amount { get; private set; }
}

/// <summary>Everything the user sets in the projection (UC14), amounts in the display currency and percentages as numbers (2 = 2%).</summary>
public sealed record ProjectionParameters(
    int HorizonMonths,
    decimal Income,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal Saving,
    decimal AnnualReturn,
    decimal Inflation,
    bool InflationOn,
    IReadOnlyList<ProjectionPayment> Payments,
    IReadOnlyList<ProjectionEvent> Events,
    decimal? Goal);

/// <summary>UC14-R11: the one projection each user saves. The rules of UC14-R9 are checked here too.</summary>
public sealed class ProjectionSettings : Entity
{
    public const int MinHorizonMonths = 1;
    public const int MaxHorizonMonths = 360;
    public const int MaxEvents = 10;
    public const int MaxNoteLength = 40;
    public const decimal MinAnnualReturn = -10m;
    public const decimal MaxAnnualReturn = 20m;
    public const decimal MinInflation = 0m;
    public const decimal MaxInflation = 20m;

    private readonly List<ProjectionPayment> _payments = [];
    private readonly List<ProjectionEvent> _events = [];

    private ProjectionSettings(Guid id, OwnerId ownerId)
        : base(id, ownerId)
    {
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private ProjectionSettings()
    {
    }

    public int HorizonMonths { get; private set; }

    public decimal Income { get; private set; }

    public decimal FixedExpenses { get; private set; }

    public decimal VariableExpenses { get; private set; }

    public decimal Saving { get; private set; }

    public decimal AnnualReturn { get; private set; }

    public decimal Inflation { get; private set; }

    public bool InflationOn { get; private set; }

    public decimal? Goal { get; private set; }

    public IReadOnlyList<ProjectionPayment> Payments => _payments;

    public IReadOnlyList<ProjectionEvent> Events => _events;

    public static ProjectionSettings Save(OwnerId ownerId, ProjectionParameters parameters)
    {
        var settings = new ProjectionSettings(Guid.NewGuid(), ownerId);
        settings.Replace(parameters);
        return settings;
    }

    public void Replace(ProjectionParameters parameters)
    {
        Check(parameters);
        HorizonMonths = parameters.HorizonMonths;
        Income = parameters.Income;
        FixedExpenses = parameters.FixedExpenses;
        VariableExpenses = parameters.VariableExpenses;
        Saving = parameters.Saving;
        AnnualReturn = parameters.AnnualReturn;
        Inflation = parameters.Inflation;
        InflationOn = parameters.InflationOn;
        Goal = parameters.Goal;
        _payments.Clear();
        _payments.AddRange(parameters.Payments.Select(payment => new ProjectionPayment(payment.DebtId, payment.Amount)));
        _events.Clear();
        _events.AddRange(parameters.Events.Select(item => new ProjectionEvent(item.Month, item.Amount, item.Note.Trim())));
    }

    public ProjectionParameters ToParameters() => new(
        HorizonMonths, Income, FixedExpenses, VariableExpenses, Saving, AnnualReturn, Inflation, InflationOn, Payments, Events, Goal);

    /// <summary>UC14-R9.</summary>
    public static bool IsValidHorizon(int months) => months is >= MinHorizonMonths and <= MaxHorizonMonths;

    /// <summary>UC14-R9: incomes, expenses, saving and payments are amounts in cents, never negative.</summary>
    public static bool IsValidMonthlyAmount(decimal amount) => amount >= 0 && Money.HasValidPrecision(amount);

    private static void Check(ProjectionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!IsValidHorizon(parameters.HorizonMonths))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "The horizon is out of range.");
        }

        var amounts = new[] { parameters.Income, parameters.FixedExpenses, parameters.VariableExpenses, parameters.Saving }
            .Concat(parameters.Payments.Select(payment => payment.Amount));
        if (!amounts.All(IsValidMonthlyAmount))
        {
            throw new ArgumentException("Monthly amounts must be non-negative amounts in cents.", nameof(parameters));
        }

        if (parameters.AnnualReturn is < MinAnnualReturn or > MaxAnnualReturn || parameters.Inflation is < MinInflation or > MaxInflation)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "A percentage is out of range.");
        }

        if (parameters.Goal is { } goal && (goal <= 0 || !Money.HasValidPrecision(goal)))
        {
            throw new ArgumentException("The goal must be a positive amount in cents.", nameof(parameters));
        }

        if (parameters.Events.Count > MaxEvents)
        {
            throw new ArgumentException("Too many events.", nameof(parameters));
        }

        foreach (var item in parameters.Events)
        {
            var note = item.Note?.Trim();
            if (item.Month < 1 || item.Month > parameters.HorizonMonths || item.Amount == 0 || !Money.HasValidPrecision(item.Amount)
                || string.IsNullOrEmpty(note) || note.Length > MaxNoteLength)
            {
                throw new ArgumentException("An event is not valid.", nameof(parameters));
            }
        }

        if (parameters.Payments.Select(payment => payment.DebtId).Distinct().Count() != parameters.Payments.Count)
        {
            throw new ArgumentException("A debt has more than one payment.", nameof(parameters));
        }
    }
}
