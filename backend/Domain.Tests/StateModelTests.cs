using System.Text.Json;
using CCC.Base.Dcb;
using CCC.Domain.Events;
using CCC.Domain.State;
using CCC.Domain.Types;

namespace CCC.Domain.Tests;

public class StateModelTests
{
    [Fact]
    public void Reading_a_state_model_before_it_is_loaded_throws()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var name = Tenant.Name(tenantId);

        var error = Assert.Throws<InvalidOperationException>(() => name.Value);
        Assert.Equal(
            $"The state model of TenantRegistered, TenantRenamed tagged tenant-id={tenantId.Value} was read before it was loaded.",
            error.Message);
    }

    [Fact]
    public void Reading_a_combined_state_model_before_it_is_loaded_throws()
    {
        var allExist = Subscription.AllExist([]);

        Assert.Throws<InvalidOperationException>(() => allExist.Value);
    }

    [Fact]
    public void A_state_model_loaded_without_records_has_its_initial_state()
    {
        var name = Tenant.Name(TenantId.From(Guid.NewGuid()));
        var allExist = Subscription.AllExist([]);

        name.Load([]);
        allExist.Load([]);

        Assert.Null(name.Value);
        Assert.True(allExist.Value);
    }

    [Fact]
    public void Loading_a_state_model_again_folds_from_its_initial_state()
    {
        var teamId = TeamId.From(Guid.NewGuid());
        var keysAdded = Team.KeysAdded(teamId);
        EventRecord[] records =
        [
            new(1, nameof(KeyAdded), JsonSerializer.Serialize(new KeyAdded(Guid.NewGuid(), "title", Guid.NewGuid())),
                DateTimeOffset.UtcNow, Guid.Empty, Tags.From(Tag.TeamId(teamId)).ToArray()),
        ];

        keysAdded.Load(records);
        keysAdded.Load(records);

        Assert.Equal(KeyCount.From(1), keysAdded.Value);
    }
}
