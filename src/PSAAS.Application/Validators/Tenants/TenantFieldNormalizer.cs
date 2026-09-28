using System.Text;

namespace PSAAS.Application.Validators.Tenants;

public static class TenantFieldNormalizer
{
    public static string NormalizeCompanyName(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return string.Empty;
        }

        var normalized = companyName.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                builder.Append(rune.ToString().ToLowerInvariant());
            }
        }

        return builder.ToString();
    }

    public static string NormalizeCompleteVatNumber(string? completeVatNumber)
    {
        return completeVatNumber?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static string NormalizeCertifiedEmail(string? certifiedEmail)
    {
        return certifiedEmail?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
