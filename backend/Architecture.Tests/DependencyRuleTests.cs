using System.Reflection;
using NetArchTest.Rules;

namespace CCC.Architecture.Tests;

public class DependencyRuleTests
{
    private static readonly Assembly Abstractions = Assembly.Load("Abstractions");
    private static readonly Assembly Api = Assembly.Load("Api");
    private static readonly Assembly Commands = Assembly.Load("Commands");
    private static readonly Assembly Domain = Assembly.Load("Domain");
    private static readonly Assembly EventStore = Assembly.Load("EventStore");
    private static readonly Assembly[] Modules = [Assembly.Load("TranslationDelivery")];

    [Fact]
    public void Abstractions_depend_on_nothing()
    {
        AssertNoDependency(Types.InAssembly(Abstractions),
            "CCC.Domain", "CCC.EventStore", "CCC.Commands", "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void EventStore_knows_nothing_about_the_domain()
    {
        AssertNoDependency(Types.InAssembly(EventStore),
            "CCC.Abstractions", "CCC.Domain", "CCC.Commands", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void Domain_depends_on_nothing_but_the_event_store()
    {
        AssertNoDependency(Types.InAssembly(Domain),
            "CCC.Abstractions", "CCC.Commands", "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Commands_do_not_depend_on_ASP_NET_Core()
    {
        AssertNoDependency(Types.InAssembly(Commands), "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Modules_depend_on_neither_commands_nor_ASP_NET_Core_nor_each_other()
    {
        foreach (var module in Modules)
        {
            var otherModules = Modules.Where(other => other != module).Select(RootNamespace);
            AssertNoDependency(Types.InAssembly(module),
                ["CCC.Commands", "CCC.Api", "Microsoft.AspNetCore", .. otherModules]);
        }
    }

    [Fact]
    public void Endpoints_go_through_handlers()
    {
        AssertNoDependency(
            Types.InAssembly(Api).That().DoNotHaveName("Program"),
            "CCC.EventStore", "CCC.Domain.State");
    }

    private static string RootNamespace(Assembly module) => $"CCC.Modules.{module.GetName().Name}";

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
