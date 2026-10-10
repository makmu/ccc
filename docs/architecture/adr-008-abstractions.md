# ADR-008: An Abstractions Project for the Handler Contracts

> **Changed by ADR-009:** `Commands` and the read model slices are now modules. `Api` and every module reference `Abstractions`.

## Decision

A new project, `Abstractions` (namespace `CCC.Abstractions`), holds the contracts every vertical slice shares at its boundary to `Api`:

- `ICommandHandler<in TCommand>`, which returns a `CommandResult`
- `IQueryHandler<in TQuery, TResult>`
- `CommandResult`, `CommandError` and `CommandErrorKind`

`Abstractions` references no other project. `Commands`, every read model slice and `Api` reference it. `Domain` and `EventStore` do not:

```
Api ──► Commands ──────────► Domain ──► EventStore
 │  └─► PublicTranslations ──┘
 │        │          │
 └────────┴──────────┴──► Abstractions
```

Queries now work like commands. A slice registers each query handler under its `IQueryHandler<TQuery, TResult>` interface, and the endpoint injects the interface. Query handlers are `internal`, like command handlers, so a slice publishes only its query records, its result records and its `Add<Slice>()` extension. A query that can find nothing returns `null`, and the endpoint maps that to 404.

`Architecture.Tests` checks that `Abstractions` depends on no `CCC.*` namespace and not on ASP.NET Core, and that neither `Domain` nor `EventStore` depends on `Abstractions`.

## Context

`ICommandHandler<T>` and `CommandResult` lived in `Commands`. That let every command handler stay internal behind one interface, and gave every command endpoint the same shape. Query handlers had no such interface. `Api` injected the concrete `TranslationsOfProjectHandler`, which therefore had to be public.

A shared query interface could not go into `Commands`, because slices must not reference it. `EventStore` was ruled out too, because endpoints must not reference it. `Domain` would have worked, since `Api` and every slice reference it. But `Domain` holds what the business is about: events, state models and value objects. How a use case is invoked and how it reports back is not a domain concern.

## Consequences

**Positive:**
- Commands and queries are symmetric: handlers are internal, endpoints inject an interface, and registration looks the same.
- A slice's public surface is its query and result records plus `Add<Slice>()`.
- `Domain` stays free of application plumbing.
- Because `Abstractions` can't see any domain type or implementation, it can't turn into a place for shared helpers.

**Negative:**
- One more project.
- Every query endpoint names both type parameters, e.g. `IQueryHandler<TranslationsOfProject, IReadOnlyList<Translation>?>`.
- `CommandResult` moved namespace, so every handler, command endpoint and command test imports `CCC.Abstractions`.

## Alternatives Considered

1. **The interfaces in `Domain`**
   - *Advantages:* No new project. Every consumer already references `Domain`.
   - *Disadvantages:* It mixes application contracts into the domain model.

2. **A `Common`, `Shared` or `Infrastructure` project**
   - *Advantages:* Familiar names.
   - *Disadvantages:* `Common` and `Shared` don't say what belongs in the project, so they attract unrelated helpers. In layered terms, infrastructure means adapters to technology, which `EventStore` already is.

3. **No query handler interface, with endpoints injecting the concrete handler**
   - *Advantages:* Less indirection, shorter endpoint signatures.
   - *Disadvantages:* Query handlers have to be public, and queries look different from commands for no reason.

4. **Endpoints querying a slice's tables through `IQueryable<T>`**
   - *Advantages:* Consumers can filter, sort and page freely, with no query record per use case.
   - *Disadvantages:* The query logic moves out of the slice and into `Api`, including the tenant scoping that keeps teams apart. The table shape becomes a public contract, Dapper has no `IQueryable`, and expressions that can't be translated only fail at runtime.

## Related Decisions

- ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects
- ADR-007: Read Models as Projections, One Project per Slice
