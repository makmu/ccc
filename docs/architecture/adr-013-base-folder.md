# ADR-013: A Base Folder for the Shared Projects

## Decision

The projects that every module builds on, other than `Domain` and `EventStore`, move into the folder `backend/Base`, which is also a solution folder:

| Project | Namespace | Holds |
|---------|-----------|-------|
| `Abstractions` | `CCC.Base.Abstractions` | The handler contracts and `CommandResult` (ADR-008) |
| `Dcb` | `CCC.Base.Dcb` | State models, tags, event filters and event records (ADR-012) |
| `Projections` | `CCC.Base.Projections` | The projection runner, `IProjection` and `ProjectionDatabase` (ADR-011) |
| `TestKit` | `CCC.Base.TestKit` | The fixtures and the in-memory event store that module tests share |

The namespaces follow the folder, as the modules' namespaces do (`CCC.Modules.<Module>`). `ProjectionBase` is renamed to `Projections`.

`Api`, `Domain`, `EventStore`, `Modules/` and the test projects `Domain.Tests` and `Architecture.Tests` stay where they are. References and the rules in `Architecture.Tests` don't change, apart from the names.

## Context

The backend root held the API, the domain, the event store, the module folder, the four shared projects and two test projects side by side. It didn't show which projects a module builds on and which ones make up the application.

ADR-011 rejected the name `Projections`, because a developer could expect the application's projections in it. Inside `Base`, and with `using CCC.Base.Projections;` in the code, the project reads as the base that projections are built on.

## Consequences

**Positive:**
- The backend root shows the application: `Api`, `Domain`, `EventStore` and `Modules`. The building blocks are grouped in one place.
- `CCC.Base.Projections` is short and doesn't suggest that the application's projections live there.

**Negative:**
- Every `using` of a shared project changed, and project paths are one level deeper.
- `EventStore` is shared infrastructure as well, but stays at the root. `Base` is therefore not the only place for infrastructure.

## Alternatives Considered

1. **Keep the shared projects at the backend root**
   - *Advantages:* Shorter paths and no namespace change.
   - *Disadvantages:* The root mixes building blocks with the application, and `ProjectionBase` keeps a name that reads like a base class.

2. **Move `EventStore` into `Base` too**
   - *Advantages:* All shared infrastructure in one folder.
   - *Disadvantages:* The event store is the subject of this demo. It stays next to `Domain` and `Api`, where it is easy to find.

3. **Keep the `CCC.<Project>` namespaces**
   - *Advantages:* Fewer changes.
   - *Disadvantages:* `using CCC.Projections;` could again be mistaken for the application's projections, and the namespaces wouldn't follow the folders the way the modules' do.

## Related Decisions

- ADR-008: An Abstractions Project for the Handler Contracts
- ADR-011: Projection Infrastructure in a ProjectionBase Project
- ADR-012: State Model Primitives in a Dependency-Free Dcb Project
