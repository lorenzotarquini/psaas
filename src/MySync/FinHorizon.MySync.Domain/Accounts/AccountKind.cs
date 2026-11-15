namespace FinHorizon.MySync.Domain.Accounts;

/// <summary>Stored by name: new kinds are appended, existing values never change.</summary>
public enum AccountKind
{
    CurrentAccount = 0,
    VirtualBank = 1,
    Cash = 2,
    Valuables = 3,
    Other = 4,

    /// <summary>Conto deposito: savings parked at a bank, often for a fixed term.</summary>
    DepositAccount = 5,

    /// <summary>Investimenti: securities, funds, ETFs held outside a savings plan.</summary>
    Investments = 6,

    /// <summary>PAC (piano di accumulo): regular contributions into a fund.</summary>
    SavingsPlan = 7,

    /// <summary>Moneta virtuale: cryptocurrencies such as bitcoin; its currency comes from the crypto catalog (UC13).</summary>
    VirtualCurrency = 8
}
