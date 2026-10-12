using CCC.Base.ModuleHosting;
using CCC.EventStore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Base.Projections;

sealed class ProjectionHealthCheck<TProjection>(
    TProjection projection,
    IEventStore eventStore,
    ModuleDatabase database) : BookmarkHealthCheck(eventStore, database)
    where TProjection : IProjection
{
    protected override string Schema => projection.Schema;

    protected override IReadOnlyCollection<string> EventTypes => projection.Handlers.EventTypes;

    protected override string NotInstalledDescription => "Not installed yet, so its queries fail.";

    public override async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var installedVersion = await QueryBookmarkAsync<int?>("version", cancellationToken);
        var health = await base.CheckHealthAsync(context, cancellationToken);
        if (installedVersion is null)
            return health;

        var data = new Dictionary<string, object> { ["version"] = installedVersion };
        foreach (var (key, value) in health.Data)
            data[key] = value;

        if (installedVersion != projection.Version)
            return HealthCheckResult.Unhealthy(
                $"Version {projection.Version} is not installed, so its queries answer from version {installedVersion}.", data: data);

        return new HealthCheckResult(health.Status, health.Description, health.Exception, data);
    }
}
