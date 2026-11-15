using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Preferences;

/// <summary>What the user chose for the whole of MySync, on every device (UC13-R4): today only the display currency.</summary>
public sealed class UserPreferences : Entity
{
    private UserPreferences(Guid id, OwnerId ownerId, string displayCurrency)
        : base(id, ownerId)
    {
        DisplayCurrency = displayCurrency;
    }

    /// <summary>For materialization by the persistence layer only.</summary>
    private UserPreferences()
    {
        DisplayCurrency = null!;
    }

    /// <summary>A traditional currency: net worth, debts and trend are shown in it (UC13).</summary>
    public string DisplayCurrency { get; private set; }

    public static UserPreferences Choose(OwnerId ownerId, string displayCurrency)
    {
        Check(displayCurrency);
        return new UserPreferences(Guid.NewGuid(), ownerId, displayCurrency);
    }

    public void ChangeDisplayCurrency(string displayCurrency)
    {
        Check(displayCurrency);
        DisplayCurrency = displayCurrency;
    }

    private static void Check(string displayCurrency)
    {
        if (!CurrencyRules.IsWellFormed(displayCurrency))
        {
            throw new ArgumentException("Not a currency code.", nameof(displayCurrency));
        }
    }
}
