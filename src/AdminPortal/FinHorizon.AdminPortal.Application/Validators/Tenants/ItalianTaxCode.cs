using System.Text.RegularExpressions;

namespace FinHorizon.AdminPortal.Application.Validators.Tenants;

/// <summary>Italian personal tax code (codice fiscale): 16 characters, the last one a check letter.</summary>
public static partial class ItalianTaxCode
{
    // Values of the characters in odd positions (1st, 3rd…); digits count as the letter with the same index.
    private static readonly int[] OddValues =
        [1, 0, 5, 7, 9, 13, 15, 17, 19, 21, 2, 4, 18, 20, 11, 3, 6, 8, 12, 14, 16, 10, 22, 25, 24, 23];

    /// <param name="value">Already trimmed and uppercase.</param>
    public static bool IsValid(string value)
    {
        if (!FormatRegex().IsMatch(value))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 15; i++)
        {
            var c = value[i];
            var index = char.IsDigit(c) ? c - '0' : c - 'A';
            sum += i % 2 == 0 ? OddValues[index] : index;
        }

        return value[15] == (char)('A' + sum % 26);
    }

    // Surname, name, year, month, day, town, check; digits may be replaced by LMNPQRSTUV (omocodia).
    [GeneratedRegex("^[A-Z]{6}[0-9LMNPQRSTUV]{2}[ABCDEHLMPRST][0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{3}[A-Z]$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatRegex();
}
