using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>UC10: the new state of an account; the opening day does not change.</summary>
public sealed record UpdateAccountCommand(Guid Id, string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance) : IAccountInput;

public sealed class UpdateAccountValidator : AccountInputValidator<UpdateAccountCommand>;

/// <summary>
/// UC10: edits an account of the current user. Balances and the net worth trend follow on their own,
/// because they are computed from the initial balance and the movements.
/// </summary>
public sealed class UpdateAccountHandler(ICurrentUser user, IAccountRepository accounts, ILedgerReader ledger, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAccountCommand, AccountView>
{
    private const string NameField = "name";

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

        account.Update(EnumNames.Parse<AccountKind>(request.Kind), descriptor, new Money(request.InitialBalance!.Value));
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<AccountView>.Invalid(NameField, ErrorCodes.NotUnique);
        }

        var movements = await ledger.MovementsByAccountAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<AccountView>.Ok(AccountView.From(account, movements.GetValueOrDefault(account.Id)));
    }
}
