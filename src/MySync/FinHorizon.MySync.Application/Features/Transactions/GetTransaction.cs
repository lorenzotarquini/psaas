using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;

namespace FinHorizon.MySync.Application.Features.Transactions;

public sealed record GetTransactionQuery(Guid Id);

public sealed class GetTransactionHandler(ICurrentUser user, ITransactionReader reader)
    : IRequestHandler<GetTransactionQuery, TransactionView>
{
    public async Task<Result<TransactionView>> HandleAsync(GetTransactionQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var view = await reader.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        return view is null ? Result<TransactionView>.NotFound() : Result<TransactionView>.Ok(view);
    }
}
