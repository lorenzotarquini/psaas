using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// Everything a rule needs to judge a movement: the data entered and the user's own records it points to.
/// A record that does not exist, or belongs to another user, is null (RNF2).
/// </summary>
public sealed record TransactionContext(
    TransactionData Data,
    Account? Account,
    Category? Category,
    Debt? Debt,
    Money DebtResidualWithoutThisTransaction,
    DateOnly Today);
