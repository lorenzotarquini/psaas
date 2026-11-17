using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Projection;
using FluentValidation;
using FluentValidation.Results;

namespace FinHorizon.MySync.Application.Features.Projection;

public sealed record ProjectionPaymentInput(Guid? DebtId, decimal? Amount);

public sealed record ProjectionEventInput(int? Month, decimal? Amount, string? Note);

/// <summary>UC14 step 10: what the user saves; percentages as numbers (2 = 2%).</summary>
public sealed record SaveProjectionCommand(
    int? HorizonMonths,
    decimal? Income,
    decimal? FixedExpenses,
    decimal? VariableExpenses,
    decimal? Saving,
    decimal? AnnualReturn,
    decimal? Inflation,
    bool InflationOn,
    IReadOnlyList<ProjectionPaymentInput>? Payments,
    IReadOnlyList<ProjectionEventInput>? Events,
    decimal? Goal);

/// <summary>UC14-R9, field by field (UC14-S10).</summary>
public sealed class SaveProjectionValidator : AbstractValidator<SaveProjectionCommand>
{
    public SaveProjectionValidator()
    {
        RuleFor(command => command.HorizonMonths)
            .NotNull().WithErrorCode(ErrorCodes.Required)
            .Must(months => months is null || ProjectionSettings.IsValidHorizon(months.Value)).WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(command => command.Income).MonthlyAmount();
        RuleFor(command => command.FixedExpenses).MonthlyAmount();
        RuleFor(command => command.VariableExpenses).MonthlyAmount();
        RuleFor(command => command.Saving).MonthlyAmount();
        RuleFor(command => command.AnnualReturn)
            .NotNull().WithErrorCode(ErrorCodes.Required)
            .Must(value => value is null or >= ProjectionSettings.MinAnnualReturn and <= ProjectionSettings.MaxAnnualReturn)
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(command => command.Inflation)
            .NotNull().WithErrorCode(ErrorCodes.Required)
            .Must(value => value is null or >= ProjectionSettings.MinInflation and <= ProjectionSettings.MaxInflation)
            .WithErrorCode(ErrorCodes.InvalidValue);
        RuleFor(command => command.Goal).OptionalPositiveCents();
        RuleFor(command => command.Events)
            .Must(events => events is null || events.Count <= ProjectionSettings.MaxEvents).WithErrorCode(ErrorCodes.InvalidValue);
        RuleForEach(command => command.Events).ChildRules(item =>
        {
            item.RuleFor(input => input.Month).NotNull().WithErrorCode(ErrorCodes.Required);
            item.RuleFor(input => input.Amount)
                .RequiredCents()
                .Must(amount => amount is null || amount.Value != 0).WithErrorCode(ErrorCodes.InvalidValue);
            item.RuleFor(input => input.Note).RequiredText(ProjectionSettings.MaxNoteLength);
        });
        // The month of an event depends on the horizon: checked against the whole command.
        RuleFor(command => command).Custom((command, context) =>
        {
            var events = command.Events ?? [];
            for (var index = 0; index < events.Count; index++)
            {
                if (events[index].Month is { } month && command.HorizonMonths is { } horizon && (month < 1 || month > horizon))
                {
                    context.AddFailure(new ValidationFailure($"Events[{index}].Month", "The event is outside the horizon.") { ErrorCode = ErrorCodes.InvalidValue });
                }
            }
        });
        RuleForEach(command => command.Payments).ChildRules(payment =>
        {
            payment.RuleFor(input => input.DebtId).NotNull().WithErrorCode(ErrorCodes.Required);
            payment.RuleFor(input => input.Amount).MonthlyAmount();
        });
    }
}

internal static class ProjectionRules
{
    /// <summary>UC14-R9: required, in cents, never negative.</summary>
    public static IRuleBuilderOptions<T, decimal?> MonthlyAmount<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule
            .RequiredCents()
            .Must(value => value is null || value.Value >= 0).WithErrorCode(ErrorCodes.InvalidValue);
}

/// <summary>UC14 step 10: replaces the saved projection of the user; a payment of a debt that is not the user's is refused.</summary>
public sealed class SaveProjectionHandler(ICurrentUser user, IProjectionRepository projections, IDebtRepository debts, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveProjectionCommand, SavedProjectionView>
{
    public async Task<Result<SavedProjectionView>> HandleAsync(SaveProjectionCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payments = request.Payments ?? [];
        var own = (await debts.ListAsync(user.Id, cancellationToken).ConfigureAwait(false)).Select(debt => debt.Id).ToHashSet();
        var errors = payments
            .Select((payment, index) => (payment, index))
            .Where(item => !own.Contains(item.payment.DebtId!.Value)
                || payments.Take(item.index).Any(other => other.DebtId == item.payment.DebtId))
            .Select(item => new FieldError($"payments[{item.index}].debtId", ErrorCodes.InvalidValue))
            .ToList();
        if (errors.Count > 0)
        {
            return Result<SavedProjectionView>.Invalid(errors);
        }

        var parameters = new ProjectionParameters(
            request.HorizonMonths!.Value,
            request.Income!.Value,
            request.FixedExpenses!.Value,
            request.VariableExpenses!.Value,
            request.Saving!.Value,
            request.AnnualReturn!.Value,
            request.Inflation!.Value,
            request.InflationOn,
            payments.Select(payment => new ProjectionPayment(payment.DebtId!.Value, payment.Amount!.Value)).ToList(),
            (request.Events ?? []).Select(item => new ProjectionEvent(item.Month!.Value, item.Amount!.Value, item.Note!)).ToList(),
            request.Goal);

        var settings = await projections.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            settings = ProjectionSettings.Save(user.Id, parameters);
            projections.Add(settings);
        }
        else
        {
            settings.Replace(parameters);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<SavedProjectionView>.Ok(SavedProjectionView.From(settings));
    }
}

public sealed record DeleteProjectionCommand;

/// <summary>UC14-10b "Riparti dai miei dati": the saved projection is gone, also when there was none.</summary>
public sealed class DeleteProjectionHandler(ICurrentUser user, IProjectionRepository projections, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectionCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(DeleteProjectionCommand request, CancellationToken cancellationToken = default)
    {
        var settings = await projections.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (settings is not null)
        {
            projections.Remove(settings);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<Unit>.Ok(Unit.Value);
    }
}
