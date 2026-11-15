using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>UC10: the new state of an account; the opening day does not change.</summary>
public sealed record UpdateAccountCommand(Guid Id, string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance, string? Currency = null) : IAccountInput;

public sealed class UpdateAccountValidator(ICurrencyCatalog catalog) : AccountInputValidator<UpdateAccountCommand>(catalog);

/// <summary>
/// UC10: edits an account of the current user. Balances and the net worth trend follow on their own,
/// because they are computed from the initial balance and the movements.
/// </summary>
public sealed class UpdateAccountHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    ILedgerReader ledger,
    CurrencyConverter converter,
    ICalendar calendar,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAccountCommand, AccountView>
{
    private const string NameField = "name";
    private const string CurrencyField = "currency";

    public async Task<Result<AccountView>> HandleAsync(UpdateAccountCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await accounts.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Result<AccountView>.NotFound();
        }

        var descriptor = new Descriptor(request.Name!, request.Description, request.Tag);
        if (await accounts.NameExistsAsync(user.Id, descriptor.NormalizedName, account.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result<AccountView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var kind = EnumNames.Parse<AccountKind>(request.Kind);
        var display = await converter.DisplayCurrencyAsync(user.Id, cancellationToken).ConfigureAwait(false);
        // Missing currency: the current one while it fits the kind, otherwise the display currency.
        var currency = request.Currency ?? (CurrencyRules.TypeFor(kind) == account.CurrencyType ? account.Currency : display);
        if (!account.KeepsCurrency(kind, currency) && await ledger.HasMovementsAsync(user.Id, account.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result<AccountView>.Invalid(CurrencyField, ErrorCodes.InvalidValue);
        }

        account.Update(kind, descriptor, currency, new Money(request.InitialBalance!.Value), hasMovements: false);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<AccountView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var movements = await ledger.MovementsByAccountAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<AccountView>.Ok(AccountView.From(account, movements.GetValueOrDefault(account.Id), new RateTable(display, []), calendar.Today));
    }
}
