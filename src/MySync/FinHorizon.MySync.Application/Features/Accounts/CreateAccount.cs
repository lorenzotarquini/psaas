using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FluentValidation;

namespace FinHorizon.MySync.Application.Features.Accounts;

/// <summary>The fields of an account the user enters, shared by creating (UC2) and editing (UC10).</summary>
public interface IAccountInput
{
    string? Kind { get; }

    string? Name { get; }

    string? Description { get; }

    string? Tag { get; }

    decimal? InitialBalance { get; }
}

/// <summary>UC2: what the user enters for a new account.</summary>
public sealed record CreateAccountCommand(string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance) : IAccountInput;

/// <summary>UC2-R1..R4: the fields on their own.</summary>
public abstract class AccountInputValidator<T> : AbstractValidator<T>
    where T : IAccountInput
{
    protected AccountInputValidator()
    {
        RuleFor(command => command.Kind).RequiredEnumName<T, AccountKind>();
        RuleFor(command => command.Name).RequiredText(Descriptor.MaxNameLength);
        RuleFor(command => command.Description).OptionalText(Descriptor.MaxDescriptionLength);
        RuleFor(command => command.Tag).OptionalText(Descriptor.MaxTagLength);
        RuleFor(command => command.InitialBalance).RequiredCents();
    }
}

public sealed class CreateAccountValidator : AccountInputValidator<CreateAccountCommand>;
