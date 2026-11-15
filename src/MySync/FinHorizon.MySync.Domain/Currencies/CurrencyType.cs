using FinHorizon.MySync.Domain.Accounts;

namespace FinHorizon.MySync.Domain.Currencies;

/// <summary>Stored by name. Traditional currencies (EUR, USD) and cryptocurrencies (BTC, ETH) live in separate catalogs (UC13).</summary>
public enum CurrencyType
{
    Fiat = 0,
    Crypto = 1
}

/// <summary>UC13: which currencies an account or debt can use and with how many decimals.</summary>
public static class CurrencyRules
{
    /// <summary>The display currency until the user chooses one (UC13-R4) and the currency of the accounts created before UC13.</summary>
    public const string Euro = "EUR";

    /// <summary>The most decimals any currency has: what the database stores.</summary>
    public const int MaxDecimals = 8;

    /// <summary>UC13-R2.</summary>
    public static int DecimalsOf(CurrencyType type) => type == CurrencyType.Crypto ? MaxDecimals : 2;

    /// <summary>Only the "Moneta virtuale" accounts hold cryptocurrencies; every other account and every debt a traditional currency.</summary>
    public static CurrencyType TypeFor(AccountKind kind) => kind == AccountKind.VirtualCurrency ? CurrencyType.Crypto : CurrencyType.Fiat;

    /// <summary>The shape of a code (ISO 4217 or a crypto ticker such as AAVE); whether it exists is the catalog's job.</summary>
    public static bool IsWellFormed(string? code) =>
        code is { Length: >= 3 and <= 10 } && code.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character));

    public static bool HasValidPrecision(decimal amount, CurrencyType type) => decimal.Round(amount, DecimalsOf(type)) == amount;
}
