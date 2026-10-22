using NLog;
using PSAAS.Application.Abstractions.Profiles;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Users;

namespace PSAAS.Application.Services.Users;

/// <summary>A row of the UC20 file. Profiles: "Cliente:Profilo" pairs separated by "|".</summary>
public sealed record UserImportRow(string? FirstName, string? LastName, string? Email, string? BirthDate, string? Profiles);

public sealed record UserImportRequest(IReadOnlyList<UserImportRow>? Rows, bool Confirm);

/// <param name="Row">1-based, as in the file without its header.</param>
/// <param name="Errors">UC6 codes per field; "profiles" for a pair that does not exist.</param>
/// <param name="Created">Set when the row was imported (confirm only).</param>
public sealed record UserImportRowResult(int Row, string Email, IReadOnlyDictionary<string, string> Errors, UserReadModel? Created);

/// <summary>UC20: preview of a CSV of users with the errors of every row, then creation of the valid rows with UC6.</summary>
public sealed class UserImportService(IProfileReadRepository profiles, UserCreateService create)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxRows = 500;

    /// <returns>Null when the file has no rows or more than <see cref="MaxRows"/>.</returns>
    public async Task<IReadOnlyList<UserImportRowResult>?> ImportAsync(UserImportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rows = request.Rows ?? [];
        if (rows.Count is 0 or > MaxRows) return null;

        // R1: "Cliente:Profilo" by name, ignoring case.
        var byName = (await profiles.GetAllAsync(cancellationToken))
            .ToLookup(p => Key(p.TenantCompanyName, p.Name), p => p.Id);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<UserImportRowResult>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var pairs = (row.Profiles ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ids = pairs.Select(pair => pair.Split(':', 2, StringSplitOptions.TrimEntries))
                .Select(parts => parts.Length == 2 ? byName[Key(parts[0], parts[1])].FirstOrDefault() : null)
                .ToArray();
            var createRequest = new UserCreateRequest(row.FirstName, row.LastName, row.Email, row.BirthDate, ids.OfType<string>().ToArray());
            var (_, _, errors) = await create.ValidateAsync(createRequest, cancellationToken);
            // profileIds[i] errors read better on the column of the file.
            foreach (var key in errors.Keys.Where(k => k.StartsWith("profileIds", StringComparison.Ordinal)).ToArray())
            {
                errors["profiles"] = errors[key];
                errors.Remove(key);
            }

            if (ids.Any(id => id is null)) errors["profiles"] = TenantFieldValidationErrorCodes.InvalidValue;
            var email = UserRules.Trim(row.Email).ToLowerInvariant();
            if (email.Length > 0 && !seen.Add(email)) errors["email"] = TenantFieldValidationErrorCodes.NotUnique;

            UserReadModel? created = null;
            if (request.Confirm && errors.Count == 0)
            {
                var result = await create.CreateAsync(createRequest, cancellationToken);
                created = result.User;
                foreach (var (field, code) in result.Errors) errors[field] = code;
            }

            results.Add(new UserImportRowResult(i + 1, email, errors, created));
        }

        Logger.Info(
            "User import of {RowCount} rows ({Mode}): {ValidCount} valid, {CreatedCount} created",
            rows.Count, request.Confirm ? "confirm" : "preview", results.Count(r => r.Errors.Count == 0), results.Count(r => r.Created is not null));
        return results;
    }

    private static string Key(string tenant, string profile) => $"{tenant.Trim().ToUpperInvariant()}\n{profile.Trim().ToUpperInvariant()}";
}
