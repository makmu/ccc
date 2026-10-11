# ADR-011: Read Model Infrastructure in Its Own Project

## Decision

The infrastructure that every module with read models shares moves from `EventStore` into a new `ReadModels` project, with the namespace `CCC.ReadModels`:

- `IProjection`, `ProjectionRunner<T>` and `ProjectionRetryPolicy`
- `ReadModelDatabase`
- `CommandDefinitionExtensions`, the Dapper helper that adds a transaction and a cancellation token to a `CommandDefinition`
- the `AddReadModelDatabase` and `AddProjection<T>` registrations

`ReadModels` references only `EventStore`. The runner needs `IEventStore.ReadAsync`, `EventRecord` and `AppendSignal`, and projections register their handlers with `EventHandlers<T>`. Every module and `Api` reference `ReadModels`:

```
Api ──► each module ──► Domain ──────► EventStore
 │          │                              ▲
 └──────────┴──────► ReadModels ───────────┘
```

Some types stay in `EventStore`:

- **`EventHandlers<T>`**, because state models use it as well as projections.
- **`AppendSignal`**, because `PostgresEventStore` sends it after every commit. It is now public, so that the runner in `ReadModels` can subscribe to it. `Notify` stays internal, so that only the event store can wake the runners.

`Architecture.Tests` checks the new direction. `ReadModels` must not depend on the domain, on `Abstractions`, on a module or on `Api`. `EventStore` and `Domain` must not depend on `ReadModels`, and endpoints must not use it.

## Context

ADR-007 put the projection infrastructure into `EventStore`, because more than one read model slice needed it and none of it knew about the domain. Since then, `EventStore` has also gained the projection stall handling (ADR-010) and a Dapper helper for transactions and cancellation tokens. Neither has anything to do with the event log.

`Domain` references `EventStore` for `StateModel<T>` and `Tags`. Through that reference, the domain also pulled in a background service, the read model database and `Microsoft.Extensions.Hosting.Abstractions`. In a demo of dynamic consistency boundaries, `EventStore` should show the event log, state models and the conditional append, and nothing else.

## Consequences

**Positive:**
- `EventStore` contains only the event log, state models, the conditional append and the signal after an append. It no longer depends on `Microsoft.Extensions.Hosting.Abstractions`.
- `Domain` no longer carries projection infrastructure along.
- Database helpers for read models have a home that isn't the event store.

**Negative:**
- One more project, which every module and `Api` reference.
- `AppendSignal` is public, although only the event store and the runners use it.
- Projections still use `CCC.EventStore` for `EventRecord` and `EventHandlers<T>`, so a projection file has both `using` lines.

## Alternatives Considered

1. **Leave the infrastructure in `EventStore`**
   - *Advantages:* No new project, and `AppendSignal` stays internal.
   - *Disadvantages:* `EventStore` keeps mixing the event log with read model concerns, and every new database helper for projections lands there too.

2. **Move only `CommandDefinitionExtensions` into another project**
   - *Advantages:* Smallest change.
   - *Disadvantages:* No reference goes away, because projections still need `IProjection` and `ReadModelDatabase` from `EventStore`. It would split the read model infrastructure across two projects.

3. **Move `EventRecord`, `EventHandlers<T>` and `AppendSignal` into a shared contracts project as well**
   - *Advantages:* `ReadModels` wouldn't need `EventStore` at all.
   - *Disadvantages:* The runner reads events through `IEventStore` anyway, so the reference would stay. It would be a third project for types that belong to the event log.

## Related Decisions

- ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects
- ADR-007: Read Models as Projections, One Project per Slice
- ADR-010: Projections Stall After Repeated Failures
