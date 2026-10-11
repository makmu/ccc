using CCC.Base.Abstractions;
using CCC.Base.Dcb;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TenantAdministration.Queries;

public record DetailsOfTenant(TenantId TenantId);

public record TenantDetails(Guid Id, string Name, IReadOnlyList<TenantSubscriptionModel> SubscriptionModels);

public record TenantSubscriptionModel(Guid Id, string Name);

sealed class DetailsOfTenantHandler(IStateLoader loader) : IQueryHandler<DetailsOfTenant, TenantDetails?>
{
    public async Task<TenantDetails?> HandleAsync(DetailsOfTenant query, CancellationToken cancellationToken)
    {
        var name = Tenant.Name(query.TenantId);
        var subscriptionModelIds = Tenant.SubscriptionModels(query.TenantId);
        await loader.LoadAsync([name, subscriptionModelIds], cancellationToken);

        if (name.IsNull(out var tenantName))
            return null;

        var subscriptionModelNames = subscriptionModelIds.List().Select(Subscription.Name).ToList();
        await loader.LoadAsync(subscriptionModelNames, cancellationToken);

        var subscriptionModels = subscriptionModelIds.List()
            .Zip(subscriptionModelNames, (id, model) => new TenantSubscriptionModel(
                id.Value,
                model.Value?.Value ?? throw new InvalidOperationException($"Subscription model {id.Value} has no name.")))
            .OrderBy(model => model.Name, StringComparer.Ordinal)
            .ToList();
        return new TenantDetails(query.TenantId.Value, tenantName.Value, subscriptionModels);
    }
}
