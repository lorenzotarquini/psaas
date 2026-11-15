using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>UC5 and UC9: an expense, an income or a transfer between two accounts.</summary>
public sealed record RecordTransactionCommand(
    string? Kind,
    DateOnly? Date,
    decimal? Amount,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    string? Note,
    Guid? CounterAccountId = null) : ITransactionInput;

public sealed class RecordTransactionValidator : TransactionInputValidator<RecordTransactionCommand>;

public sealed class RecordTransactionHandler(
    ICurrentUser user,
    TransactionContextLoader contextLoader,
    TransactionPolicy policy,
    ITransactionRepository transactions,
    ITransactionReader reader,
    TimeProvider clock,
    IUnitOfWork unitOfWork) : IRequestHandler<RecordTransactionCommand, TransactionView>
{
    public async Task<Result<TransactionView>> HandleAsync(RecordTransactionCommand request, CancellationToken cancellationToken = default)
    {
        var data = request.ToData();
        var context = await contextLoader.LoadAsync(user.Id, data, amendedTransactionId: null, cancellationToken).ConfigureAwait(false);
        var errors = policy.Check(context);
        if (errors.Count > 0)
        {
            return Result<TransactionView>.Invalid(errors);
        }

        var transaction = Transaction.Record(user.Id, data, clock.GetUtcNow());
        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var view = await reader.GetAsync(user.Id, transaction.Id, cancellationToken).ConfigureAwait(false);
        return Result<TransactionView>.Created(view ?? throw new InvalidOperationException("A recorded movement cannot be read back."));
    }
}
