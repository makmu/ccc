using System.Reflection;
using CCC.Abstractions;
using CCC.ReadModels;
using NetArchTest.Rules;

namespace CCC.Architecture.Tests;

public class DependencyRuleTests
{
    private static readonly Assembly Abstractions = Assembly.Load("Abstractions");
    private static readonly Assembly Api = Assembly.Load("Api");
    private static readonly Assembly Domain = Assembly.Load("Domain");
    private static readonly Assembly EventStore = Assembly.Load("EventStore");
    private static readonly Assembly ReadModels = Assembly.Load("ReadModels");
    private static readonly Assembly[] Modules =
    [
        Assembly.Load("AppAdministration"),
        Assembly.Load("TenantAdministration"),
        Assembly.Load("TeamAdministration"),
        Assembly.Load("TranslationEditor"),
        Assembly.Load("TranslationDelivery"),
    ];

    [Fact]
    public void Abstractions_depend_on_nothing()
    {
        AssertNoDependency(Types.InAssembly(Abstractions),
            "CCC.Domain", "CCC.EventStore", "CCC.ReadModels", "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void EventStore_knows_nothing_about_the_domain_or_the_read_models()
    {
        AssertNoDependency(Types.InAssembly(EventStore),
            "CCC.Abstractions", "CCC.Domain", "CCC.ReadModels", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void ReadModels_know_nothing_about_the_domain()
    {
        AssertNoDependency(Types.InAssembly(ReadModels),
            "CCC.Abstractions", "CCC.Domain", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void Domain_depends_on_nothing_but_the_event_store()
    {
        AssertNoDependency(Types.InAssembly(Domain),
            "CCC.Abstractions", "CCC.ReadModels", "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Modules_depend_on_neither_the_API_nor_ASP_NET_Core_nor_each_other()
    {
        foreach (var module in Modules)
        {
            var otherModules = Modules.Where(other => other != module).Select(RootNamespace);
            AssertNoDependency(Types.InAssembly(module),
                ["CCC.Api", "Microsoft.AspNetCore", .. otherModules]);
        }
    }

    [Fact]
    public void Endpoints_go_through_handlers()
    {
        AssertNoDependency(
            Types.InAssembly(Api).That().DoNotHaveName("Program"),
            "CCC.EventStore", "CCC.ReadModels", "CCC.Domain.State");
    }

    [Fact]
    public void Handlers_are_reachable_only_through_their_interfaces()
    {
        var handlers = Modules
            .SelectMany(module => module.GetTypes())
            .Where(type => type.GetInterfaces().Any(IsHandlerInterface))
            .ToList();

        Assert.NotEmpty(handlers);
        Assert.Empty(handlers.Where(type => type.IsVisible).Select(type => type.FullName));
    }

    [Fact]
    public void Commands_and_queries_live_in_their_own_namespaces()
    {
        var misplaced = Modules
            .SelectMany(module => module.GetTypes())
            .Where(type => Side(type) is { } side && !type.Namespace!.EndsWith($".{side}"))
            .Select(type => type.FullName);

        Assert.Empty(misplaced);

        foreach (var module in Modules)
        {
            foreach (var side in new[] { "Commands", "Queries" })
            {
                var result = Types.InAssembly(Api)
                    .That().HaveDependencyOn($"{RootNamespace(module)}.{side}")
                    .Should().ResideInNamespaceEndingWith($".{side}")
                    .GetResult();
                Assert.True(result.IsSuccessful,
                    $"Endpoints of {side.ToLower()} outside a {side} namespace: " + string.Join(", ", result.FailingTypeNames ?? []));
            }
        }
    }

    private static string RootNamespace(Assembly module) => $"CCC.Modules.{module.GetName().Name}";

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
            || type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

    private static string? Side(Type type)
    {
        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            return "Commands";
        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            || typeof(IProjection).IsAssignableFrom(type))
            return "Queries";
        return null;
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
