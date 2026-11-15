using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Debts;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Debts;

/// <summary>UC3: what the user enters for a new debt.</summary>
public sealed record CreateDebtCommand(string? Kind, string? Name, string? Description, string? Tag, Guid? AccountId, decimal? Residual);

/// <summary>UC3-R1..R3: the fields on their own.</summary>
public sealed class CreateDebtValidator : AbstractValidator<CreateDebtCommand>
{
    public CreateDebtValidator()
    {
        RuleFor(command => command.Kind).RequiredEnumName<CreateDebtCommand, DebtKind>();
        RuleFor(command => command.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(command => command.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(command => command.Tag).OptionalText(Descriptor.MaxTagLength);
        RuleFor(command => command.AccountId).Required();
        RuleFor(command => command.Residual).RequiredPositiveCents();
    }
}
