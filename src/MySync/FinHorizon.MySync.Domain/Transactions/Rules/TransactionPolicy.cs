using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Domain.Transactions.Rules;

/// <summary>
/// Composite of every <see cref="ITransactionRule"/>: one error per field, the first rule that speaks wins.
/// </summary>
public sealed class TransactionPolicy
{
    private readonly IReadOnlyList<ITransactionRule> _rules;

    public TransactionPolicy(IEnumerable<ITransactionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToList();
    }

    public IReadOnlyList<FieldError> Check(TransactionContext context) =>
        _rules
            .SelectMany(rule => rule.Check(context))
            .DistinctBy(error => error.Field)
            .ToList();
}
