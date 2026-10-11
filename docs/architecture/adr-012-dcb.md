# ADR-012: State Model Primitives in a Dependency-Free Dcb Project

> **Changed by ADR-013:** `Dcb` now lives in `backend/Base/Dcb`, with the namespace `CCC.Base.Dcb`, and `ProjectionBase` is now `Projections`.

## Decision

The building blocks of dynamic consistency boundaries move from `EventStore` into a new project `Dcb`, with the namespace `CCC.Dcb`:

- `StateModel<T>` and `StateModel`, `IStateModel` and `AllStateModel`
- `EventFilter`, `EventRecord` and its `PayloadAs` extension
- `EventHandlers<T>`
- `Tags`

`Dcb` references no project and no package. It uses only the base class library, such as `System.Text.Json` and a generated regex.

`Domain` references only `Dcb`. `EventStore` references `Dcb` instead of being referenced by `Domain`:

```
Api ──► each module ──► Domain ──► Dcb
           │                        ▲
           ├──► EventStore ─────────┤
           └──► ProjectionBase ─────┘
                     └──► EventStore
```

`IEventStore`, `PostgresEventStore`, `StateLoader`, `StateContext`, `AppendSignal` and the schema setup stay in `EventStore`, because they are the persistence side.

`Architecture.Tests` checks that `Dcb` depends on no `CCC.*` namespace, on neither Dapper nor Npgsql, and on no `Microsoft.*` package. It checks that `Domain` depends on nothing but `Dcb`, and that endpoints don't use `Dcb`.

## Context

ADR-006 put `StateModel<T>`, `IStateModel` and `Tags` into `EventStore`, so `Domain` referenced `EventStore` to define its state models. Through that reference, the domain depended on Dapper, Npgsql and the PostgreSQL implementation of the event store. The domain should stand on its own and only be referenced.

Moving the state model types into `Domain` would have removed every dependency of `Domain`. But `EventStore` would then have referenced `Domain` to load state models, and the rule that the event store knows nothing about the domain would have held only for some namespaces.

## Consequences

**Positive:**
- `Domain` no longer depends on the event store implementation, Dapper or Npgsql. It depends only on `Dcb`, which depends on nothing.
- `EventStore` and `ProjectionBase` still know nothing about the domain.
- `Dcb` shows the DCB concepts on their own: state models, tags, filters and event records, without any storage.

**Negative:**
- One more project. Modules reference `Dcb`, `Domain`, `EventStore` and `ProjectionBase`.
- Most command handlers and projections have one more `using` line.

## Alternatives Considered

1. **Leave the state model types in `EventStore` (ADR-006)**
   - *Advantages:* No new project.
   - *Disadvantages:* The domain depends on the PostgreSQL implementation of the event store.

2. **Move the state model types into `Domain`**
   - *Advantages:* `Domain` depends on nothing at all.
   - *Disadvantages:* `EventStore` and `ProjectionBase` would reference `Domain`, so they could reach events, state models and value objects. An architecture rule would have to allow one namespace of `Domain` and forbid the others.

## Related Decisions

- ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects
- ADR-011: Projection Infrastructure in a ProjectionBase Project
