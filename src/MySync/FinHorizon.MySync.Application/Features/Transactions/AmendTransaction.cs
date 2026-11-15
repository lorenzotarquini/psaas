using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Transactions.Rules;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>UC8: the new state of an existing movement.</summary>
public sealed record AmendTransactionCommand(
    Guid Id,
    string? Kind,
    DateOnly? Date,
    decimal? Amount,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    string? Note) : ITransactionInput;

public sealed class AmendTransactionValidator : TransactionInputValidator<AmendTransactionCommand>;

/// <summary>
/// UC8-R1: amending is judged as deleting and recording again; the residual check leaves out the movement itself (M3b).
/// </summary>
public sealed class AmendTransactionHandler(
    ICurrentUser user,
    TransactionContextLoader contextLoader,
    TransactionPolicy policy,
    ITransactionRepository transactions,
    ITransactionReader reader,
    IUnitOfWork unitOfWork) : IRequestHandler<AmendTransactionCommand, TransactionView>
{
    public async Task<Result<TransactionView>> HandleAsync(AmendTransactionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = await transactions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            return Result<TransactionView>.NotFound();
        }

        var data = request.ToData();
        var context = await contextLoader.LoadAsync(user.Id, data, transaction.Id, cancellationToken).ConfigureAwait(false);
        var errors = policy.Check(context);
        if (errors.Count > 0)
        {
            return Result<TransactionView>.Invalid(errors);
        }

        transaction.Amend(data);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var view = await reader.GetAsync(user.Id, transaction.Id, cancellationToken).ConfigureAwait(false);
        return Result<TransactionView>.Ok(view ?? throw new InvalidOperationException("An amended movement cannot be read back."));
    }
}
