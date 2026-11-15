namespace FinHorizon.MySync.Domain.NetWorth;

/// <summary>
/// The groups the views of the portfolio can show or hide (UC12). Every account and debt belongs to exactly one.
/// </summary>
public enum PortfolioGroup
{
    /// <summary>Day-to-day money and debts: always shown.</summary>
    Everyday = 0,

    /// <summary>Stable assets and long-term debts: valuables, mortgages.</summary>
    Stable = 1,

    /// <summary>Investments and deposits: deposit accounts, investments, savings plans (PAC).</summary>
    Investments = 2
}
