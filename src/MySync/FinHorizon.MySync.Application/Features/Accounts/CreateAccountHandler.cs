using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>UC2: opens an account for the current user, within the limit and with a name of its own.</summary>
public sealed class CreateAccountHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IResourceLimits limits,
    ICalendar calendar,
    CurrencyConverter converter,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateAccountCommand, AccountView>
{
    public async Task<Result<AccountView>> HandleAsync(CreateAccountCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var descriptor = new Descriptor(request.Name!, request.Description, request.Tag);

        var limit = (await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false)).MaxAccounts;
        // shortcut: two concurrent creations can both pass the count; acceptable for one person, add a lock if limits become paid features.
        if (await accounts.CountAsync(user.Id, cancellationToken).ConfigureAwait(false) >= limit)
        {
            return Result<AccountView>.Invalid(LimitField.Name, ErrorCodes.LimitReached);
        }

        if (await accounts.NameExistsAsync(user.Id, descriptor.NormalizedName, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            return Result<AccountView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var display = await converter.DisplayCurrencyAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var account = Account.Open(
            user.Id,
            EnumNames.Parse<AccountKind>(request.Kind),
            descriptor,
            request.Currency ?? display,
            new Money(request.InitialBalance!.Value),
            calendar.Today);
        accounts.Add(account);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<AccountView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        // No rate service here: the dashboard converts, the creation only needs to succeed.
        return Result<AccountView>.Created(AccountView.From(account, Money.Zero, new RateTable(display, []), calendar.Today));
    }

    private const string NameField = "name";
}
