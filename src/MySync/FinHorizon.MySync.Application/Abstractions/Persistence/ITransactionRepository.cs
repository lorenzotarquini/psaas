using FinHorizon.MySync.Domain.Transactions;

namespace FinHorizon.MySync.Application.Abstractions.Persistence;

public interface ITransactionRepository : IOwnedRepository<Transaction>
{
    /// <summary>Permanent: there is no bin in the MVP (UC8-R4).</summary>
    void Remove(Transaction transaction);
}
