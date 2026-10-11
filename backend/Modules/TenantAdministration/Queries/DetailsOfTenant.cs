using CCC.Abstractions;
using CCC.Domain.State;
using CCC.Domain.Types;
using CCC.EventStore;

namespace CCC.Modules.TenantAdministration.Queries;

public record DetailsOfTenant(TenantId TenantId);

public record TenantDetails(Guid Id, string Name, IReadOnlyList<TenantSubscriptionModel> SubscriptionModels);

public record TenantSubscriptionModel(Guid Id, string Name);

sealed class DetailsOfTenantHandler(IStateLoader loader) : IQueryHandler<DetailsOfTenant, TenantDetails?>
{
    public async Task<TenantDetails?> HandleAsync(DetailsOfTenant query)
    {
        var (name, subscriptionModelIds, _) = await loader.LoadAsync(
            Tenant.Name(query.TenantId),
            Tenant.SubscriptionModels(query.TenantId));

        if (name is not { } tenantName)
            return null;

        var subscriptionModelNames = subscriptionModelIds.Select(Subscription.Name).ToList();
        await loader.LoadAsync(subscriptionModelNames);

        var subscriptionModels = subscriptionModelIds
            .Zip(subscriptionModelNames, (id, model) => new TenantSubscriptionModel(id.Value, model.State?.Value!))
            .OrderBy(model => model.Name)
            .ToList();
        return new TenantDetails(query.TenantId.Value, tenantName.Value, subscriptionModels);
    }
}
