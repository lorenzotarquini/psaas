using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Subscriptions;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Subscriptions;

public sealed record ListSubscriptionsQuery;

/// <summary>UC16 step 3.</summary>
public sealed record CreateSubscriptionCommand(string? Name, string? Description, Guid? AccountId, Guid? CategoryId, decimal? Amount, DateOnly? FirstChargeOn)
    : ISubscriptionInput;

public sealed class CreateSubscriptionValidator : SubscriptionInputValidator<CreateSubscriptionCommand>;

/// <summary>UC16 flow M.</summary>
public sealed record UpdateSubscriptionCommand(Guid Id, string? Name, string? Description, Guid? AccountId, Guid? CategoryId, decimal? Amount, DateOnly? FirstChargeOn)
    : ISubscriptionInput;

public sealed class UpdateSubscriptionValidator : SubscriptionInputValidator<UpdateSubscriptionCommand>;

/// <summary>UC16 flow A.</summary>
public sealed record CancelSubscriptionCommand(Guid Id, DateOnly? EndsOn);

public sealed class CancelSubscriptionValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionValidator() => RuleFor(command => command.EndsOn).Required();
}

/// <summary>UC16 flow A1.</summary>
public sealed record ResumeSubscriptionCommand(Guid Id);

/// <summary>UC16-5a.</summary>
public sealed record SkipSubscriptionChargeCommand(Guid Id, DateOnly? DueOn);

public sealed class SkipSubscriptionChargeValidator : AbstractValidator<SkipSubscriptionChargeCommand>
{
    public SkipSubscriptionChargeValidator() => RuleFor(command => command.DueOn).Required();
}

/// <summary>UC16 flow E.</summary>
public sealed record DeleteSubscriptionCommand(Guid Id);

public sealed class ListSubscriptionsHandler(ICurrentUser user, SubscriptionReader reader) : IRequestHandler<ListSubscriptionsQuery, SubscriptionList>
{
    public async Task<Result<SubscriptionList>> HandleAsync(ListSubscriptionsQuery request, CancellationToken cancellationToken = default) =>
        Result<SubscriptionList>.Ok(await reader.ListAsync(user.Id, cancellationToken).ConfigureAwait(false));
}

/// <summary>UC16 steps 3–4: no movement is recorded (R18).</summary>
public sealed class CreateSubscriptionHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    SubscriptionTargets targets,
    SubscriptionReader reader,
    IResourceLimits limits,
    ICalendar calendar,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSubscriptionCommand, SubscriptionView>
{
    public async Task<Result<SubscriptionView>> HandleAsync(CreateSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var limit = (await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false)).MaxSubscriptions;
        if (await subscriptions.CountOpenAsync(user.Id, cancellationToken).ConfigureAwait(false) >= limit)
        {
            return Result<SubscriptionView>.Invalid(LimitField.Name, ErrorCodes.LimitReached);
        }

        var (account, errors) = await targets.CheckAsync(user.Id, request, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Result<SubscriptionView>.Invalid(errors);
        }

        var descriptor = new Descriptor(request.Name!, request.Description, null);
        if (await subscriptions.NameExistsAsync(user.Id, descriptor.NormalizedName, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            return Result<SubscriptionView>.Invalid(SubscriptionFields.Name, ErrorCodes.NotUnique);
        }

        var subscription = Subscription.Start(
            user.Id, descriptor, account.Id, account.Currency, request.CategoryId!.Value, new Money(request.Amount!.Value), request.FirstChargeOn!.Value, calendar.Today);
        subscriptions.Add(subscription);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<SubscriptionView>.Invalid(SubscriptionFields.Name, ErrorCodes.NotUnique);
        }

        return Result<SubscriptionView>.Created(await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>UC16-R12: past expenses never change; with linked expenses the currency and the first charge are bound by them.</summary>
public sealed class UpdateSubscriptionHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    SubscriptionTargets targets,
    SubscriptionReader reader,
    ILedgerReader ledger,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateSubscriptionCommand, SubscriptionView>
{
    public async Task<Result<SubscriptionView>> HandleAsync(UpdateSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscription = await subscriptions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<SubscriptionView>.NotFound();
        }

        var (account, errors) = await targets.CheckAsync(user.Id, request, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Result<SubscriptionView>.Invalid(errors);
        }

        var payments = await ledger.SubscriptionPaymentsAsync(user.Id, subscription.Id, cancellationToken).ConfigureAwait(false);
        var fieldErrors = new List<FieldError>();
        if (payments.Count > 0 && account.Currency != subscription.Currency)
        {
            fieldErrors.Add(new FieldError(SubscriptionFields.AccountId, ErrorCodes.InvalidValue));
        }

        var firstChargeOn = request.FirstChargeOn!.Value;
        if ((payments.Count > 0 && firstChargeOn > payments.Min(payment => payment.Date)) || firstChargeOn > subscription.EndsOn)
        {
            fieldErrors.Add(new FieldError(SubscriptionFields.FirstChargeOn, ErrorCodes.InvalidValue));
        }

        var descriptor = new Descriptor(request.Name!, request.Description, null);
        if (await subscriptions.NameExistsAsync(user.Id, descriptor.NormalizedName, subscription.Id, cancellationToken).ConfigureAwait(false))
        {
            fieldErrors.Add(new FieldError(SubscriptionFields.Name, ErrorCodes.NotUnique));
        }

        if (fieldErrors.Count > 0)
        {
            return Result<SubscriptionView>.Invalid(fieldErrors);
        }

        subscription.Update(descriptor, account.Id, account.Currency, request.CategoryId!.Value, new Money(request.Amount!.Value), firstChargeOn);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateNameException)
        {
            return Result<SubscriptionView>.Invalid(SubscriptionFields.Name, ErrorCodes.NotUnique);
        }

        return Result<SubscriptionView>.Ok(await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>UC16-R13: the end is not before the first charge nor before the last linked expense.</summary>
public sealed class CancelSubscriptionHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    SubscriptionReader reader,
    ILedgerReader ledger,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelSubscriptionCommand, SubscriptionView>
{
    public async Task<Result<SubscriptionView>> HandleAsync(CancelSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscription = await subscriptions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<SubscriptionView>.NotFound();
        }

        var endsOn = request.EndsOn!.Value;
        var payments = await ledger.SubscriptionPaymentsAsync(user.Id, subscription.Id, cancellationToken).ConfigureAwait(false);
        if (endsOn < subscription.FirstChargeOn || payments.Any(payment => payment.Date > endsOn))
        {
            return Result<SubscriptionView>.Invalid(SubscriptionFields.EndsOn, ErrorCodes.InvalidValue);
        }

        subscription.Cancel(endsOn);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SubscriptionView>.Ok(await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>UC16-R14: back without an end date, within the limit of R4.</summary>
public sealed class ResumeSubscriptionHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    SubscriptionReader reader,
    IResourceLimits limits,
    ICalendar calendar,
    IUnitOfWork unitOfWork) : IRequestHandler<ResumeSubscriptionCommand, SubscriptionView>
{
    public async Task<Result<SubscriptionView>> HandleAsync(ResumeSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscription = await subscriptions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<SubscriptionView>.NotFound();
        }

        if (subscription.IsCancelled)
        {
            var limit = (await limits.ForAsync(user.Id, cancellationToken).ConfigureAwait(false)).MaxSubscriptions;
            if (await subscriptions.CountOpenAsync(user.Id, cancellationToken).ConfigureAwait(false) >= limit)
            {
                return Result<SubscriptionView>.Invalid(LimitField.Name, ErrorCodes.LimitReached);
            }

            subscription.Resume(calendar.Today);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<SubscriptionView>.Ok(await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>UC16-R9: only the charge due right now.</summary>
public sealed class SkipSubscriptionChargeHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    SubscriptionReader reader,
    IUnitOfWork unitOfWork) : IRequestHandler<SkipSubscriptionChargeCommand, SubscriptionView>
{
    public async Task<Result<SubscriptionView>> HandleAsync(SkipSubscriptionChargeCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscription = await subscriptions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<SubscriptionView>.NotFound();
        }

        var view = await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false);
        if (view.DueOn is not { } due || due != request.DueOn)
        {
            return Result<SubscriptionView>.Invalid(SubscriptionFields.DueOn, ErrorCodes.InvalidValue);
        }

        subscription.Skip(due);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SubscriptionView>.Ok(await reader.ViewAsync(user.Id, subscription, cancellationToken).ConfigureAwait(false));
    }
}

/// <summary>UC16-R15: linked expenses are history, so only a subscription without them is deleted.</summary>
public sealed class DeleteSubscriptionHandler(
    ICurrentUser user,
    ISubscriptionRepository subscriptions,
    ILedgerReader ledger,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteSubscriptionCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(DeleteSubscriptionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscription = await subscriptions.GetAsync(user.Id, request.Id, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<Unit>.NotFound();
        }

        if ((await ledger.SubscriptionPaymentsAsync(user.Id, subscription.Id, cancellationToken).ConfigureAwait(false)).Count > 0)
        {
            return Result<Unit>.Invalid(SubscriptionFields.Payments, ErrorCodes.InvalidValue);
        }

        subscriptions.Remove(subscription);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}
