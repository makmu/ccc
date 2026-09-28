using System.Reflection;
using NetArchTest.Rules;

namespace CCC.Architecture.Tests;

public class DependencyRuleTests
{
    private static readonly Assembly Api = Assembly.Load("Api");
    private static readonly Assembly Commands = Assembly.Load("Commands");
    private static readonly Assembly Domain = Assembly.Load("Domain");
    private static readonly Assembly EventStore = Assembly.Load("EventStore");

    [Fact]
    public void EventStore_knows_nothing_about_the_domain()
    {
        AssertNoDependency(Types.InAssembly(EventStore), "Domain", "Commands", "Api");
    }

    [Fact]
    public void Domain_depends_on_nothing_but_the_event_store()
    {
        AssertNoDependency(Types.InAssembly(Domain), "Commands", "Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Commands_do_not_depend_on_ASP_NET_Core()
    {
        AssertNoDependency(Types.InAssembly(Commands), "Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Endpoints_go_through_command_handlers()
    {
        AssertNoDependency(
            Types.InAssembly(Api).That().DoNotHaveName("Program"),
            "EventStore", "Domain.State");
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
