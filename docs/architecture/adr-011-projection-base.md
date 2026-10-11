# ADR-011: Projection Infrastructure in a ProjectionBase Project

> **Changed by ADR-013:** the project is now called `Projections` and lives in `backend/Base/Projections`, with the namespace `CCC.Base.Projections`. The `Base` folder and namespace mark it as infrastructure, which answers the objection to alternative 4 below.

> **Changed by ADR-012:** `EventHandlers<T>` and `EventRecord` have moved from `EventStore` into the dependency-free `Dcb` project. `ProjectionBase` references `Dcb` and `EventStore`, and projections no longer need `CCC.EventStore`.

## Decision

The infrastructure that every module with projections shares moves from `EventStore` into a new `ProjectionBase` project, with the namespace `CCC.ProjectionBase`:

- `IProjection`, `ProjectionRunner<T>` and `ProjectionRetryPolicy`
- `ProjectionDatabase`
- `CommandDefinitionExtensions`, the Dapper helper that adds a transaction and a cancellation token to a `CommandDefinition`
- the `AddProjectionDatabase` and `AddProjection<T>` registrations

`ProjectionBase` references only `EventStore`. The runner needs `IEventStore.ReadAsync`, `EventRecord` and `AppendSignal`, and projections register their handlers with `EventHandlers<T>`. Every module and `Api` reference `ProjectionBase`:

```
Api ──► each module ──► Domain ──────► EventStore
 │          │                              ▲
 └──────────┴──────► ProjectionBase ───────┘
```

Some types stay in `EventStore`:

- **`EventHandlers<T>`**, because state models use it as well as projections.
- **`AppendSignal`**, because `PostgresEventStore` sends it after every commit. It is now public, so that the runner in `ProjectionBase` can subscribe to it. `Notify` stays internal, so that only the event store can wake the runners.

The project is named after what it is: the base that projections are built on. It holds no projection of the application. Those stay in the modules, next to their queries.

`Architecture.Tests` checks the new direction. `ProjectionBase` must not depend on the domain, on `Abstractions`, on a module or on `Api`. `EventStore` and `Domain` must not depend on `ProjectionBase`, and endpoints must not use it.

## Context

ADR-007 put the projection infrastructure into `EventStore`, because more than one projection slice needed it and none of it knew about the domain. Since then, `EventStore` has also gained the projection stall handling (ADR-010) and a Dapper helper for transactions and cancellation tokens. Neither has anything to do with the event log.

`Domain` references `EventStore` for `StateModel<T>` and `Tags`. Through that reference, the domain also pulled in a background service, the projection database and `Microsoft.Extensions.Hosting.Abstractions`. In a demo of dynamic consistency boundaries, `EventStore` should show the event log, state models and the conditional append, and nothing else.

## Consequences

**Positive:**
- `EventStore` contains only the event log, state models, the conditional append and the signal after an append. It no longer depends on `Microsoft.Extensions.Hosting.Abstractions`.
- `Domain` no longer carries projection infrastructure along.
- Database helpers for projections have a home that isn't the event store.

**Negative:**
- One more project, which every module and `Api` reference.
- `AppendSignal` is public, although only the event store and the runners use it.
- Projections still use `CCC.EventStore` for `EventRecord` and `EventHandlers<T>`, so a projection file has both `using` lines.

## Alternatives Considered

1. **Leave the infrastructure in `EventStore`**
   - *Advantages:* No new project, and `AppendSignal` stays internal.
   - *Disadvantages:* `EventStore` keeps mixing the event log with projection concerns, and every new database helper for projections lands there too.

2. **Move only `CommandDefinitionExtensions` into another project**
   - *Advantages:* Smallest change.
   - *Disadvantages:* No reference goes away, because projections still need `IProjection` and `ProjectionDatabase` from `EventStore`. It would split the projection infrastructure across two projects.

3. **Move `EventRecord`, `EventHandlers<T>` and `AppendSignal` into a shared contracts project as well**
   - *Advantages:* `ProjectionBase` wouldn't need `EventStore` at all.
   - *Disadvantages:* The runner reads events through `IEventStore` anyway, so the reference would stay. It would be a third project for types that belong to the event log.

4. **Name the project `Projections`**
   - *Advantages:* Matches the term used throughout the codebase.
   - *Disadvantages:* Suggests the project holds the application's projections, when it only holds the infrastructure that builds them.

5. **Name the project `ProjectionEngine`**
   - *Advantages:* Doesn't read like the name of an abstract base class, as `…Base` usually does in .NET.
   - *Disadvantages:* Less obvious as the foundation that every module's projections build on.

## Related Decisions

- ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects
- ADR-007: Projections Answer Queries, One Project per Slice
- ADR-010: Projections Stall After Repeated Failures
