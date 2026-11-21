using FinHorizon.MySync.Application.Abstractions.Limits;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinHorizon.MySync.Application;

public static class DependencyInjection
{
    /// <summary>
    /// The use cases of MySync. Handlers, validators and transaction rules are discovered by convention,
    /// so a new use case or rule is a new class and nothing to register by hand.
    /// </summary>
    public static IServiceCollection AddMySyncApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<PortfolioReader>()
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)), publicOnly: true)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)).Where(type => !type.IsAbstract), publicOnly: true)
            .AsImplementedInterfaces()
            .WithSingletonLifetime());
        services.Scan(scan => scan
            .FromAssemblyOf<ITransactionRule>()
            .AddClasses(classes => classes.AssignableTo<ITransactionRule>(), publicOnly: true)
            .As<ITransactionRule>()
            .WithSingletonLifetime());

        // Outermost last: logging sees the outcome of validation too.
        services.Decorate(typeof(IRequestHandler<,>), typeof(ValidationDecorator<,>));
        services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingDecorator<,>));

        services.AddSingleton<TransactionPolicy>();
        services.AddScoped<TransactionContextLoader>();
        services.AddScoped<PortfolioReader>();
        services.AddScoped<Features.Subscriptions.SubscriptionTargets>();
        services.AddScoped<Features.Subscriptions.SubscriptionReader>();
        services.AddScoped<Features.Currencies.CurrencyConverter>();
        services.AddScoped<Features.Memberships.MembershipSync>();
        services.AddOptions<Features.Memberships.MembershipOptions>();
        // Defaults here; the infrastructure binds and validates the configuration section.
        services.AddOptions<LimitsOptions>();
        // Contract C2/C6 (docs/architettura-moduli.md): the limits of the tier paid, configuration as fallback.
        services.AddSingleton<Features.Memberships.IModuleTiers, Features.Memberships.DeclaredModuleTiers>();
        services.AddScoped<IResourceLimits, Features.Memberships.TierResourceLimits>();
        return services;
    }
}
