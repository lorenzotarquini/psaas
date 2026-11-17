using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Abstractions.Reading;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Currencies;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Domain.Projection;

namespace FinHorizon.MySync.Application.Features.Projection;

public sealed record GetProjectionQuery;

/// <summary>
/// UC14 steps 2–3: the starting point, the suggested parameters and the saved ones. The projection itself is computed by
/// the frontend (D16), so every change of a parameter is immediate.
/// </summary>
public sealed class GetProjectionHandler(
    ICurrentUser user,
    PortfolioReader portfolio,
    IDebtRepository debts,
    ISubscriptionRepository subscriptions,
    IProjectionReader reader,
    IProjectionRepository projections,
    CurrencyConverter converter,
    ICalendar calendar) : IRequestHandler<GetProjectionQuery, ProjectionView>
{
    /// <summary>UC14-R3: the starting values of the two percentages, which the user then changes at will.</summary>
    public const decimal DefaultAnnualReturn = 2m;

    public const decimal DefaultInflation = 2m;

    public async Task<Result<ProjectionView>> HandleAsync(GetProjectionQuery request, CancellationToken cancellationToken = default)
    {
        var today = calendar.Today;
        // UC14-R1 and D15: always every account and debt, whatever the switches of UC12.
        var current = await portfolio.ReadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var start = new ProjectionStartView(
            SumOf(current, PortfolioGroup.Everyday),
            SumOf(current, PortfolioGroup.Investments),
            SumOf(current, PortfolioGroup.Stable));

        var window = ProjectionWindow.For(today, current.Accounts.Count == 0 ? null : current.Accounts.Min(account => account.OpenedOn));
        var flows = window.IsEmpty
            ? []
            : await reader.FlowsAsync(user.Id, window.From, window.To, cancellationToken).ConfigureAwait(false);
        var debtList = await debts.ListAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var activeViews = current.Debts.Where(view => !view.PaidOff).ToList();
        // UC14-R3: the subscriptions not ended before today; their end date stops them month by month (R13).
        var subscriptionList = (await subscriptions.ListAsync(user.Id, cancellationToken).ConfigureAwait(false))
            .Where(subscription => subscription.IsActiveOn(today))
            .OrderBy(subscription => subscription.Descriptor.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var currencies = flows.Select(flow => flow.Currency).Concat(debtList.Select(debt => debt.Currency)).Concat(subscriptionList.Select(subscription => subscription.Currency));
        var rates = await converter.RatesAsync(current.Currency, currencies, window.IsEmpty ? today : window.From, today, cancellationToken).ConfigureAwait(false);

        decimal Total(ProjectionFlowKind kind, Guid? debtId = null, DateOnly? from = null) =>
            flows.Where(flow => flow.Kind == kind && (debtId is null || flow.DebtId == debtId) && (from is null || flow.Date >= from))
                .Sum(flow => rates.ToDisplay(flow.Currency, flow.Amount, flow.Date));

        var debtViews = new List<ProjectionDebtView>();
        var payments = new List<ProjectionPaymentView>();
        foreach (var view in activeViews)
        {
            var debt = debtList.Single(item => item.Id == view.Id);
            decimal? given = debt.MonthlyPayment is { } payment ? RateTable.Round(rates.ToDisplay(debt.Currency, payment.Amount, today)) : null;
            debtViews.Add(new ProjectionDebtView(view.Id, view.Name, view.DisplayResidual ?? 0m, given));
            // UC14-R3: the monthly payment when given, otherwise the installments of the last months.
            payments.Add(new ProjectionPaymentView(
                view.Id,
                given ?? ProjectionWindow.Average(Total(ProjectionFlowKind.Installment, view.Id, window.InstallmentsFrom), window.InstallmentMonths)));
        }

        var subscriptionViews = subscriptionList
            .Select(subscription => new ProjectionSubscriptionView(
                subscription.Id,
                subscription.Descriptor.Name,
                RateTable.Round(rates.ToDisplay(subscription.Currency, subscription.Amount.Amount, today)),
                subscription.FirstChargeOn,
                subscription.EndsOn))
            .ToList();

        var suggested = new SuggestedParametersView(
            ProjectionWindow.Average(Total(ProjectionFlowKind.Income), window.Months),
            ProjectionWindow.Average(Total(ProjectionFlowKind.FixedExpense), window.Months),
            ProjectionWindow.Average(Total(ProjectionFlowKind.VariableExpense), window.Months),
            Math.Max(0m, ProjectionWindow.Average(Total(ProjectionFlowKind.Saving), window.Months)),
            DefaultAnnualReturn,
            DefaultInflation,
            payments,
            subscriptionViews.Select(view => new ProjectionSubscriptionParameterView(view.Id, view.Amount, true)).ToList());

        var saved = await projections.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var active = activeViews.Select(view => view.Id).ToHashSet();
        return Result<ProjectionView>.Ok(new ProjectionView(
            today,
            start,
            debtViews,
            subscriptionViews,
            suggested,
            window.Months,
            saved is null ? null : SavedProjectionView.From(saved, active, subscriptionViews.Select(view => view.Id).ToHashSet()),
            current.Currency));
    }

    private static decimal SumOf(Portfolio.Portfolio current, PortfolioGroup group) =>
        current.Accounts.Where(account => account.Group == group).Sum(account => account.DisplayBalance ?? 0m);
}
