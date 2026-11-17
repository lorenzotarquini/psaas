using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Debts;

namespace FinHorizon.MySync.Application.Features.Debts;

/// <summary>UC3: records a debt of the current user, linked to one of the user's accounts.</summary>
public sealed class CreateDebtHandler(
    ICurrentUser user,
    IDebtRepository debts,
    IAccountRepository accounts,
    IResourceLimits limits,
    ICalendar calendar,
    CurrencyConverter converter,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateDebtCommand, DebtView>
{
    private const string NameField = "name";
    private const string AccountField = "accountId";

    public async Task<Result<DebtView>> HandleAsync(CreateDebtCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var descriptor = new Descriptor(request.Name!, request.Description, request.Tag);

        var limit = (await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false)).MaxDebts;
        if (await debts.CountAsync(user.Id, cancellationToken).ConfigureAwait(false) >= limit)
        {
            return Result<DebtView>.Invalid(LimitField.Name, ErrorCodes.LimitReached);
        }

        var account = await accounts.GetAsync(user.Id, request.AccountId!.Value, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Result<DebtView>.Invalid(AccountField, ErrorCodes.InvalidValue);
        }

        if (await debts.NameExistsAsync(user.Id, descriptor.NormalizedName, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            return Result<DebtView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var display = await converter.DisplayCurrencyAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var debt = Debt.Take(
            user.Id,
            EnumNames.Parse<DebtKind>(request.Kind),
            descriptor,
            account.Id,
            request.Currency ?? display,
            new Money(request.Residual!.Value),
            calendar.Today,
            request.MonthlyPayment is { } payment ? new Money(payment) : null);
        debts.Add(debt);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<DebtView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        return Result<DebtView>.Created(DebtView.From(debt, Money.Zero, new RateTable(display, []), calendar.Today));
    }
}
