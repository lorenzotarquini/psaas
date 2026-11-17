using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Abstractions.Payments;
using FinHorizon.MySync.Application.Abstractions.Persistence;
using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Domain.Memberships;
using Microsoft.Extensions.Options;

namespace FinHorizon.MySync.Application.Features.Memberships;

public sealed record GetMembershipQuery;

/// <summary>UC17 step 2: email and name come from the token, for the Stripe customer (R2).</summary>
public sealed record StartCheckoutCommand(string? Email, string? Name);

/// <summary>UC17 flow 1a.</summary>
public sealed record OpenBillingPortalCommand;

/// <summary>UC17 step 4: back from Stripe, the state is read at once instead of waiting for the event.</summary>
public sealed record RefreshMembershipCommand;

public static class MembershipErrors
{
    public const string Field = "membership";

    /// <summary>UC17 flow 2b.</summary>
    public const string AlreadyActive = "AlreadyActive";

    /// <summary>The portal needs a Stripe customer: the user never paid.</summary>
    public const string NoCustomer = "NoCustomer";
}

public sealed class GetMembershipHandler(ICurrentUser user, IMembershipRepository memberships, IOptions<MembershipOptions> options)
    : IRequestHandler<GetMembershipQuery, MembershipView>
{
    public async Task<Result<MembershipView>> HandleAsync(GetMembershipQuery request, CancellationToken cancellationToken = default) =>
        Result<MembershipView>.Ok(MembershipView.From(await memberships.GetAsync(user.Id, cancellationToken).ConfigureAwait(false), options.Value));
}

public sealed class StartCheckoutHandler(
    ICurrentUser user,
    IMembershipRepository memberships,
    IPaymentProvider payments,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    IOptions<MembershipOptions> options) : IRequestHandler<StartCheckoutCommand, RedirectView>
{
    public async Task<Result<RedirectView>> HandleAsync(StartCheckoutCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var membership = await memberships.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (membership?.IsOpen == true)
        {
            return Result<RedirectView>.Invalid(MembershipErrors.Field, MembershipErrors.AlreadyActive);
        }

        if (membership is null)
        {
            // R2: the customer is created once and kept, so every later payment and invoice is on the same customer.
            var customerId = await payments.CreateCustomerAsync(user.Id, request.Email, request.Name, cancellationToken).ConfigureAwait(false);
            membership = new Membership(user.Id, customerId, clock.GetUtcNow());
            memberships.Add(membership);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var url = await payments.CreateCheckoutAsync(membership.CustomerId, user.Id, options.Value.ReturnUrl("paid"), cancellationToken).ConfigureAwait(false);
        return Result<RedirectView>.Ok(new RedirectView(url));
    }
}

public sealed class OpenBillingPortalHandler(
    ICurrentUser user,
    IMembershipRepository memberships,
    IPaymentProvider payments,
    IOptions<MembershipOptions> options) : IRequestHandler<OpenBillingPortalCommand, RedirectView>
{
    public async Task<Result<RedirectView>> HandleAsync(OpenBillingPortalCommand request, CancellationToken cancellationToken = default)
    {
        if (await memberships.GetAsync(user.Id, cancellationToken).ConfigureAwait(false) is not { } membership)
        {
            return Result<RedirectView>.Invalid(MembershipErrors.Field, MembershipErrors.NoCustomer);
        }

        var url = await payments.CreatePortalAsync(membership.CustomerId, options.Value.ReturnUrl("portal"), cancellationToken).ConfigureAwait(false);
        return Result<RedirectView>.Ok(new RedirectView(url));
    }
}

public sealed class RefreshMembershipHandler(
    ICurrentUser user,
    IMembershipRepository memberships,
    MembershipSync sync,
    IOptions<MembershipOptions> options) : IRequestHandler<RefreshMembershipCommand, MembershipView>
{
    public async Task<Result<MembershipView>> HandleAsync(RefreshMembershipCommand request, CancellationToken cancellationToken = default)
    {
        var membership = await memberships.GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (membership is not null)
        {
            await sync.SyncAsync(membership, cancellationToken).ConfigureAwait(false);
        }

        return Result<MembershipView>.Ok(MembershipView.From(membership, options.Value));
    }
}
