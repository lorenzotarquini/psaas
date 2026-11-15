using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>UC2: what the user enters for a new account.</summary>
public sealed record CreateAccountCommand(string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance);

/// <summary>UC2-R1..R4: the fields on their own.</summary>
public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(command => command.Kind).RequiredEnumName<CreateAccountCommand, AccountKind>();
        RuleFor(command => command.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(command => command.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(command => command.Tag).OptionalText(Descriptor.MaxTagLength);
        RuleFor(command => command.InitialBalance).RequiredCents();
    }
}
