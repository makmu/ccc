using System.Reflection;
using CCC.Base.Abstractions;
using CCC.Base.Automations;
using CCC.Base.ModuleHosting;
using CCC.Base.Projections;
using NetArchTest.Rules;

namespace CCC.Architecture.Tests;

public class DependencyRuleTests
{
    private static readonly Assembly Abstractions = Assembly.Load("Abstractions");
    private static readonly Assembly Api = Assembly.Load("Api");
    private static readonly Assembly Automations = Assembly.Load("Automations");
    private static readonly Assembly Dcb = Assembly.Load("Dcb");
    private static readonly Assembly Domain = Assembly.Load("Domain");
    private static readonly Assembly EventStore = Assembly.Load("EventStore");
    private static readonly Assembly ModuleHosting = Assembly.Load("ModuleHosting");
    private static readonly Assembly Projections = Assembly.Load("Projections");
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
            "CCC.Base.Dcb", "CCC.Domain", "CCC.EventStore", "CCC.Base.ModuleHosting", "CCC.Base.Projections", "CCC.Base.Automations",
            "CCC.Modules", "CCC.Api", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Dcb_depends_on_nothing()
    {
        AssertNoDependency(Types.InAssembly(Dcb),
            "CCC.Base.Abstractions", "CCC.Domain", "CCC.EventStore", "CCC.Base.ModuleHosting", "CCC.Base.Projections",
            "CCC.Base.Automations", "CCC.Modules", "CCC.Api", "Dapper", "Npgsql", "Microsoft");
    }

    [Fact]
    public void EventStore_knows_nothing_about_the_domain_or_what_builds_on_it()
    {
        AssertNoDependency(Types.InAssembly(EventStore),
            "CCC.Base.Abstractions", "CCC.Domain", "CCC.Base.ModuleHosting", "CCC.Base.Projections", "CCC.Base.Automations",
            "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void ModuleHosting_knows_nothing_about_the_domain_or_the_projections_or_the_automations()
    {
        AssertNoDependency(Types.InAssembly(ModuleHosting),
            "CCC.Base.Abstractions", "CCC.Domain", "CCC.Base.Projections", "CCC.Base.Automations", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void Projections_know_nothing_about_the_domain_or_the_automations()
    {
        AssertNoDependency(Types.InAssembly(Projections),
            "CCC.Base.Abstractions", "CCC.Domain", "CCC.Base.Automations", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void Automations_know_nothing_about_the_domain_or_the_projections()
    {
        AssertNoDependency(Types.InAssembly(Automations),
            "CCC.Domain", "CCC.Base.Projections", "CCC.Modules", "CCC.Api");
    }

    [Fact]
    public void Domain_depends_on_nothing_but_Dcb()
    {
        AssertNoDependency(Types.InAssembly(Domain),
            "CCC.Base.Abstractions", "CCC.EventStore", "CCC.Base.ModuleHosting", "CCC.Base.Projections", "CCC.Base.Automations",
            "CCC.Modules", "CCC.Api", "Microsoft");
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
            "CCC.Base.Dcb", "CCC.EventStore", "CCC.Base.ModuleHosting", "CCC.Base.Projections", "CCC.Base.Automations",
            "CCC.Domain.State");
    }

    [Fact]
    public void Automations_append_nothing_themselves()
    {
        var automations = Types.InAssemblies(Modules).That().ImplementInterface(typeof(IAutomation));

        Assert.NotEmpty(automations.GetTypes());
        AssertNoDependency(automations, "CCC.EventStore", "CCC.Domain.State");
    }

    [Fact]
    public void Each_module_has_one_internal_marker_so_only_it_can_open_its_database()
    {
        foreach (var module in Modules)
        {
            var marker = Assert.Single(module.GetTypes(), type => typeof(IModule).IsAssignableFrom(type));
            Assert.False(marker.IsVisible, $"{marker.FullName} is public.");
        }
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
        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            || typeof(IAutomation).IsAssignableFrom(type))
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
