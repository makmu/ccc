using System.Reflection;
using NetArchTest.Rules;

namespace CCC.Architecture.Tests;

// The dependency rules from refactoring-plan.md. Project references already
// stop most violations; these catch the ones a reference can't, like a new
// package or an endpoint reaching past the command handlers.
public class DependencyRuleTests
{
    private static readonly Assembly Api = Assembly.Load("CCC.Api");
    private static readonly Assembly Commands = Assembly.Load("CCC.Commands");
    private static readonly Assembly Domain = Assembly.Load("CCC.Domain");
    private static readonly Assembly EventStore = Assembly.Load("CCC.EventStore");

    [Fact]
    public void EventStore_knows_nothing_about_the_domain()
    {
        AssertNoDependency(Types.InAssembly(EventStore), "CCC.Domain", "CCC.Commands", "CCC.Api");
    }

    [Fact]
    public void Domain_depends_on_nothing_but_the_event_store()
    {
        AssertNoDependency(Types.InAssembly(Domain), "CCC.Commands", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Commands_do_not_depend_on_ASP_NET_Core()
    {
        AssertNoDependency(Types.InAssembly(Commands), "CCC.Api", "Microsoft.AspNetCore");
    }

    // Program.cs registers the event store, which is the one exception.
    [Fact]
    public void Endpoints_go_through_command_handlers()
    {
        AssertNoDependency(
            Types.InAssembly(Api).That().DoNotHaveName("Program"),
            "CCC.EventStore", "CCC.Domain.State");
    }

    private static void AssertNoDependency(PredicateList types, params string[] namespaces) =>
        AssertNoDependency(types.ShouldNot().HaveDependencyOnAny(namespaces));

    private static void AssertNoDependency(Types types, params string[] namespaces) =>
        AssertNoDependency(types.ShouldNot().HaveDependencyOnAny(namespaces));

    private static void AssertNoDependency(ConditionList condition)
    {
        var result = condition.GetResult();
        Assert.True(result.IsSuccessful,
            "Forbidden dependency in: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
