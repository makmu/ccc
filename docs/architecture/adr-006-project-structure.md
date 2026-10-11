# ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects

> **Changed by ADR-009:** the use cases of `Commands` now live in modules under `Modules/`, one per area of work, and `Commands` is gone. The other projects, the dependency direction and the three decisions below still hold.

> **Changed by ADR-012:** `StateModel<T>`, `IStateModel`, `Tags` and the other state model primitives now live in a dependency-free `Dcb` project. `Domain` references only `Dcb`, and so does `EventStore`. This replaces decision 1 below and adopts a variant of alternative 2.

## Decision

The backend is split into four projects, each with one job. All four live in `backend/` and share `backend/CCC.sln`.

| Project | Contains |
|---------|----------|
| `Api` | ASP.NET Core endpoints, request DTOs, mapping requests to value objects, mapping command results to HTTP, startup |
| `Commands` | One command record and one handler per use case. A handler loads state, runs the business checks, and appends events or returns an error |
| `Domain` | Domain events, state model definitions and the tag vocabulary, value objects |
| `EventStore` | `IEventStore` and its PostgreSQL/Dapper implementation, `StateModel<T>`, `StateLoader`, the conditional append, schema setup |

References point one way only:

```
Api ──► Commands ──► Domain ──► EventStore
```

`Api` and `Commands` also reference the projects further down directly. Nothing references back up. `Architecture.Tests` enforces the rules that project references alone can't:

- `EventStore` knows nothing about tenants, keys or any other domain concept.
- `Domain` and `Commands` don't use ASP.NET Core. Handlers return a `CommandResult` (success, `NotFound`, `Conflict` or `Invalid`) and the API maps it to 200, 404, 409 or 400.
- Endpoints don't touch the state models in `CCC.Domain.State`, `Tag` or the event store. Only `Program.cs` references `EventStore`, to register it.

Three further decisions shape the code:

1. **`StateModel<T>` lives in `EventStore`.** `StateModel<T>`, `IStateModel` and `Tags` contain nothing domain-specific, so they belong with the event store. As a result `Domain` references `EventStore` to define its state models.
2. **Events keep primitive types.** Event records use `Guid` and `string`, not value objects. Stored JSON payloads therefore didn't change, and no JSON converters are needed. Handlers unwrap value objects when they build an event.
3. **Value objects check format, handlers check business rules.** A value object's `From` throws `InvalidValueException` on malformed input, such as an empty id or a blank name. An endpoint filter turns that exception into a 400 before any command exists. Handlers check the rules that need the event log, such as existence, uniqueness and limits.

## Context

Before this split, the whole backend was one project. Each endpoint loaded its state, ran the business checks, appended events and built the HTTP response inline. The same `try/catch` for concurrency conflicts appeared in all twelve endpoints. The business rules could only be exercised over HTTP against a real database. Nothing stopped an endpoint from using the event store in a way that bypassed the checks.

Names were canonicalized by calling a string extension before each tag was built. If a call site forgot it, a tag would silently hold a name that never matches the existing events.

## Consequences

**Positive:**
- Business rules can be tested without HTTP or PostgreSQL. `Commands.Tests` runs every handler against an in-memory `IEventStore`.
- Each endpoint is a few lines long: build value objects, send the command, map the result.
- `Tag` factories take value objects, so a name can't reach a tag without being canonicalized.
- Concurrency conflicts are turned into a `Conflict` result in one place, `StateContext.TryAppendAsync`.
- The planned projection slices can reference `Domain` for event types without depending on the API.

**Negative:**
- Four projects plus two test projects is more structure than twelve endpoints strictly need.
- `Domain` depends on `EventStore`, so the domain is not free of infrastructure types.
- Types used across projects must be public, which exposes more of the event store than before. `PostgresEventStore` and the handler classes stay internal.
- Event records must never be renamed, because the stored event type is the record's class name. This held before the split too, but events now live far from the code that stores them, so the rule is easier to forget.

## Alternatives Considered

1. **One project with folders per layer**
   - *Advantages:* No project references to manage, and nothing needs to be public.
   - *Disadvantages:* Nothing enforces the boundaries. The single project was already organized into folders, and endpoints still mixed HTTP, business rules and persistence.

2. **A separate `EventStore.Abstractions` project for `StateModel<T>` and `Tags`**
   - *Advantages:* The domain would depend only on abstractions, not on the Dapper/Npgsql implementation.
   - *Disadvantages:* One more project for little gain. `EventStore` has no domain dependencies, so the domain's reference to it can't create a cycle. Worth revisiting if the event store gets a second implementation.

3. **Value objects inside events**
   - *Advantages:* Type safety all the way into the event log.
   - *Disadvantages:* The stored JSON shape would change or would need custom converters, and every existing event would have to keep deserializing correctly. Keeping value objects at the command boundary was simpler.

4. **`TryFrom` or a `Result<T>` instead of exceptions for malformed input**
   - *Advantages:* No exceptions for expected input errors.
   - *Disadvantages:* Every endpoint would need one check per field. `AddKey` alone builds four value objects. The exception never gets past the endpoint filter, so it doesn't leak into the rest of the code.
