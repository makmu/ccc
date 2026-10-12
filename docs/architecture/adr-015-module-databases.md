# ADR-015: Every Module Owns Its Database, Hosted Through ModuleHosting

## Decision

Every module owns a database of its own for its projections and the bookmarks of its automations. The single projection database goes away. A new project `Base/ModuleHosting`, namespace `CCC.Base.ModuleHosting`, holds what projections and automations share, and `Projections` and `Automations` both build on it without knowing each other.

- **The module's database.** `ModuleDatabase<TModule>` replaces `ProjectionDatabase`. `TModule` is a marker type that each module declares as an internal `IModule` at its root, such as `TenantAdministrationModule`, whose static `Name` is the module's name. Query handlers inject `ModuleDatabase<TenantAdministrationModule>`. Because the marker is internal, no other module can name the type, so a module can only open its own database. A module still keeps its tables in its own schema.
- **Connection strings.** `ConnectionStrings:Modules` is the default database of every module. A connection string named after a module, such as `ConnectionStrings:TranslationDelivery`, moves that module into a database of its own. `Program.cs` passes this lookup to `AddModuleDatabases`. Modules with equal connection strings share one `NpgsqlDataSource`, which `ModuleDatabases` keeps per connection string, so each database still has one connection pool.
- **Registration.** `AddModule<TModule>()` registers the module's database, `AppendSignal`, the default retry policy and health checks, and returns a `ModuleBuilder`. `Projections` and `Automations` extend it: `services.AddModule<TenantAdministrationModule>().AddProjection<TenantAdministrationProjection>().AddAutomation<DefaultTeamAutomation>()`. The builder creates runners and health checks with the module's database, so neither needs the marker type.
- **What moves into `ModuleHosting`.** `EventLogRunner` (ADR-014), the retry policy, now `RunnerRetryPolicy`, the Dapper helper `.With`, and `BookmarkHealthCheck`, the base of both health checks. It reports a missing bookmark, a stall and events left to process. `AutomationHealthCheck<T>` only names its schema, event types and texts. `ProjectionHealthCheck<T>` also reads the installed version, adds it to the data, and reports a version that isn't installed before anything else.
- **What stays.** `IProjection`, `ProjectionRunner<T>` and `AddProjection<T>` stay in `Projections`. `IAutomation`, `Reaction`, `AutomationRunner<T>` and `AddAutomation<T>` stay in `Automations`. `AppendSignal` stays in `EventStore`. The event store keeps its own database and connection string.

`ModuleHosting` references `Dcb` and `EventStore`. `Projections` and `Automations` reference `ModuleHosting` instead of `Automations` referencing `Projections`, and `Api` references `ModuleHosting` instead of `Projections`, for `AddModuleDatabases` in `Program.cs`:

```
Projections ──┐
              ├──► ModuleHosting ──► EventStore, Dcb
Automations ──┘
```

`Architecture.Tests` checks that `ModuleHosting` knows nothing about the domain, `Abstractions`, `Projections` or `Automations`, that `Automations` doesn't depend on `Projections`, that `Abstractions`, `Dcb`, `EventStore`, `Domain` and the endpoints don't depend on `ModuleHosting`, and that each module has exactly one marker, which is internal.

## Context

Each module owned one schema, but every module shared one projection database. Ownership was modelled everywhere except in storage, and the rule that a module never reads another module's tables was only written down.

Automations made this visible (ADR-014). They kept their bookmarks in the projection database and shared the projection runner's retry and stall handling, so `Automations` referenced `Projections` for `ProjectionDatabase`, `ProjectionRetryPolicy` and `EventLogRunner`. Two of these names said projection although automations used them, and `AutomationHealthCheck<T>` repeated most of `ProjectionHealthCheck<T>`. Moving only the shared code into its own project wouldn't have helped, because `Automations` would still have needed `Projections` for the database.

A database per module is named after its owner rather than after one of its two users, and both projections and automations belong in it.

## Consequences

**Positive:**
- A module can only open its own database, and the compiler checks it.
- A module can move into a database of its own by adding one connection string, without any change to the code. Without overrides, the configuration still has two connection strings.
- `Projections` and `Automations` no longer depend on each other, and no type that automations use is named after projections.
- The retry, stall and health logic exists once.
- An automation's bookmark sits next to the projections of the same module, which is the state it belongs to.

**Negative:**
- One more project in `Base`, and one marker type per module.
- Modules that share a database still share a server and its credentials. Nothing in PostgreSQL stops a query from reading another module's schema, only the types in the code do.
- `ModuleHosting` is public, so `EventLogRunner` and `BookmarkHealthCheck` can be subclassed outside the base projects.
- A projection's health check reads its bookmark twice, once for the version and once in `BookmarkHealthCheck`.
- The `Projections` connection string is gone. A deployment that sets it fails at startup until it sets `Modules` instead.

## Alternatives Considered

1. **Move only the shared code into a new project**
   - *Advantages:* No change to the database or the query handlers.
   - *Disadvantages:* `Automations` would still reference `Projections` for `ProjectionDatabase`, or the new project would hold a type named after projections.

2. **Keep automation bookmarks in the event store database**
   - *Advantages:* `Automations` wouldn't need the projection database at all, and dropping the projection database wouldn't make automations react to the whole log again.
   - *Disadvantages:* `EventStoreDatabase` would have to be public, and the event store database would hold tables `EventStore` doesn't own. An automation's bookmark is state of the module it belongs to.

3. **One required connection string per module**
   - *Advantages:* Explicit, no fallback.
   - *Disadvantages:* Five connection strings that are equal in every current setup, and one more for every new module.

4. **Keyed services instead of marker types**
   - *Advantages:* No marker type, the module name is enough.
   - *Disadvantages:* Every query handler repeats the key as a string, and any module could inject another module's database.

5. **A generic builder, `ModuleBuilder<TModule>`, with `AddProjection<T>()` as a C# 14 extension member**
   - *Advantages:* Runners would get `ModuleDatabase<TModule>` directly from dependency injection.
   - *Disadvantages:* C# doesn't infer `TModule` from the receiver when the call names `T` explicitly, so every call would need both type arguments.

6. **PostgreSQL roles per module**
   - *Advantages:* The database itself would refuse reads of another module's schema.
   - *Disadvantages:* One role and one set of credentials per module, or a `SET ROLE` on every connection. That is more than a demo needs, and it can still be added on top of this decision.

## Related Decisions

- ADR-009: Modules by Area of Work
- ADR-010: Projections Stall After Repeated Failures
- ADR-011: Projection Infrastructure in a ProjectionBase Project
- ADR-013: A Base Folder for the Shared Projects
- ADR-014: Automations React to Events by Issuing Commands
