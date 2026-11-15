using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;

namespace FinHorizon.MySync.Application.Features.Transactions;

/// <summary>UC8: permanent deletion (R4); balances and residuals follow because they are computed.</summary>
public sealed record DeleteTransactionCommand(Guid Id);

public sealed class DeleteTransactionHandler(ICurrentUser user, ITransactionRepository transactions, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTransactionCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(DeleteTransactionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = await transactions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            return Result<Unit>.NotFound();
        }

        transactions.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}
