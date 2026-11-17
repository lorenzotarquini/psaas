using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.Transactions;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Subscriptions;

/// <summary>The fields of a subscription the user enters, shared by creating and editing (UC16 step 3, M).</summary>
public interface ISubscriptionInput
{
    string? Name { get; }

    string? Description { get; }

    Guid? AccountId { get; }

    Guid? CategoryId { get; }

    decimal? Amount { get; }

    DateOnly? FirstChargeOn { get; }
}

/// <summary>UC16-R1, R2: the fields on their own; account and category are checked against the user's records.</summary>
public abstract class SubscriptionInputValidator<T> : AbstractValidator<T>
    where T : ISubscriptionInput
{
    protected SubscriptionInputValidator()
    {
        RuleFor(input => input.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(input => input.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(input => input.AccountId).Required();
        RuleFor(input => input.CategoryId).Required();
        RuleFor(input => input.Amount).RequiredPositiveCents();
        RuleFor(input => input.FirstChargeOn).Required();
    }
}

/// <summary>Field names of a subscription, as the API exposes them.</summary>
public static class SubscriptionFields
{
    public const string Name = "name";
    public const string AccountId = "accountId";
    public const string CategoryId = "categoryId";
    public const string FirstChargeOn = "firstChargeOn";
    public const string EndsOn = "endsOn";
    public const string DueOn = "dueOn";
    public const string Payments = "payments";
}

/// <summary>UC16-R2, R3: the account is the user's, in a traditional currency; the category is an expense category of the user.</summary>
public sealed class SubscriptionTargets(IAccountRepository accounts, ICategoryRepository categories)
{
    public async Task<(Account? Account, IReadOnlyList<FieldError> Errors)> CheckAsync(OwnerId owner, ISubscriptionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var errors = new List<FieldError>();
        var account = await accounts.GetAsync(owner, input.AccountId!.Value, cancellationToken).ConfigureAwait(false);
        if (account is null || account.CurrencyType != CurrencyType.Fiat)
        {
            errors.Add(new FieldError(SubscriptionFields.AccountId, ErrorCodes.InvalidValue));
        }

        var category = await categories.GetAsync(owner, input.CategoryId!.Value, cancellationToken).ConfigureAwait(false);
        if (category is null || category.Kind != TransactionKind.Expense)
        {
            errors.Add(new FieldError(SubscriptionFields.CategoryId, ErrorCodes.InvalidValue));
        }

        return (errors.Count == 0 ? account : null, errors);
    }
}
