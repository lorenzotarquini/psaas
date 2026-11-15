using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Preferences;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Preferences;

/// <summary>
/// UC13-R4: the display currency, the euro while <see cref="Chosen"/> is false. The setup (UC1) asks for it when it was never chosen.
/// </summary>
public sealed record PreferencesView(string DisplayCurrency, bool Chosen);

public sealed record GetPreferencesQuery;

/// <summary>UC1 and UC11: the user chooses the display currency.</summary>
public sealed record UpdatePreferencesCommand(string? DisplayCurrency);

public sealed class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesValidator(ICurrencyCatalog catalog)
    {
        RuleFor(command => command.DisplayCurrency)
            .Must(code => !string.IsNullOrWhiteSpace(code)).WithErrorCode(ErrorCodes.Required)
            // A traditional currency only (UC13-R4).
            .MustAsync(async (code, cancellationToken) => code is null || await catalog.HasAsync(code, CurrencyType.Fiat, cancellationToken).ConfigureAwait(false))
            .WithErrorCode(ErrorCodes.InvalidValue);
    }
}

public sealed class GetPreferencesHandler(ICurrentUser user, IPreferencesRepository preferences) : IRequestHandler<GetPreferencesQuery, PreferencesView>
{
    public async Task<Result<PreferencesView>> HandleAsync(GetPreferencesQuery request, CancellationToken cancellationToken = default)
    {
        var current = await preferences.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<PreferencesView>.Ok(current is null
            ? new PreferencesView(CurrencyRules.Euro, Chosen: false)
            : new PreferencesView(current.DisplayCurrency, Chosen: true));
    }
}

public sealed class UpdatePreferencesHandler(ICurrentUser user, IPreferencesRepository preferences, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePreferencesCommand, PreferencesView>
{
    public async Task<Result<PreferencesView>> HandleAsync(UpdatePreferencesCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var currency = request.DisplayCurrency!;
        var current = await preferences.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            preferences.Add(UserPreferences.Choose(user.Id, currency));
        }
        else
        {
            current.ChangeDisplayCurrency(currency);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<PreferencesView>.Ok(new PreferencesView(currency, Chosen: true));
    }
}
