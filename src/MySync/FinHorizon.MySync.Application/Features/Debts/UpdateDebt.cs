using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Debts;

namespace FinHorizon.MySync.Application.Features.Debts;

/// <summary>UC10: the new state of a debt; <see cref="Residual"/> is the initial residual, the creation day does not change.</summary>
public sealed record UpdateDebtCommand(Guid Id, string? Kind, string? Name, string? Description, string? Tag, Guid? AccountId, decimal? Residual, string? Currency = null) : IDebtInput;

public sealed class UpdateDebtValidator(ICurrencyCatalog catalog) : DebtInputValidator<UpdateDebtCommand>(catalog);

/// <summary>UC10: edits a debt of the current user; the initial residual cannot go below the installments already paid (R4).</summary>
public sealed class UpdateDebtHandler(
    ICurrentUser user,
    IDebtRepository debts,
    IAccountRepository accounts,
    ILedgerReader ledger,
    CurrencyConverter converter,
    ICalendar calendar,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateDebtCommand, DebtView>
{
    private const string NameField = "name";

    public async Task<Result<DebtView>> HandleAsync(UpdateDebtCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var debt = await debts.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (debt is null)
        {
            return Result<DebtView>.NotFound();
        }

        if (await accounts.GetAsync(user.Id, request.AccountId!.Value, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<DebtView>.Invalid("accountId", ErrorCodes.InvalidValue);
        }

        var descriptor = new Descriptor(request.Name!, request.Description, request.Tag);
        if (await debts.NameExistsAsync(user.Id, descriptor.NormalizedName, debt.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result<DebtView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var paid = await ledger.InstallmentsPaidAsync(user.Id, debt.Id, null, cancellationToken).ConfigureAwait(false);
        var residual = new Money(request.Residual!.Value);
        if (residual < paid)
        {
            return Result<DebtView>.Invalid("residual", ErrorCodes.InvalidValue);
        }

        // UC13-R6: the installments already paid are in the current currency.
        var currency = request.Currency ?? debt.Currency;
        if (currency != debt.Currency && paid != Money.Zero)
        {
            return Result<DebtView>.Invalid("currency", ErrorCodes.InvalidValue);
        }

        debt.Update(EnumNames.Parse<DebtKind>(request.Kind), descriptor, request.AccountId.Value, currency, residual, paid);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<DebtView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var display = await converter.DisplayCurrencyAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<DebtView>.Ok(DebtView.From(debt, paid, new RateTable(display, []), calendar.Today));
    }
}
