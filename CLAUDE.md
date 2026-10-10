# CCC - Multi-Tenant CMS (Content/Copy Management System)

## Project Overview
CCC is a multi-tenant CMS that allows users to define keys and translations for those keys. Its primary focus is enabling developers, project managers, and editors to manage label texts for user controls in different languages. Keys are not limited to simple UI elements — they can also represent dynamic welcome texts, mail templates, or any static data consumed by a target application.

## Architecture
The system consists of two main parts:

### Frontend
- React application
- Provides the management UI for tenants, keys, and translations

### Backend (C# / ASP.NET Core)
Two distinct API surfaces:

1. **Internal API** — used exclusively by the frontend to manage tenants, keys, and translations
2. **Public API** — allows client applications to fetch key/translation results by tenant and locale

## Project Structure
All backend projects live in `backend/`, share `backend/CCC.sln`, and inherit `net10.0`, `Nullable` and `ImplicitUsings` from `backend/Directory.Build.props`. Project names have no `CCC.` prefix, but each sets `RootNamespace`, so namespaces keep it: the `Domain` project's code lives in `CCC.Domain.*`.

- `Abstractions` — the contracts every slice shares at its boundary to `Api`: `ICommandHandler<TCommand>`, `IQueryHandler<TQuery, TResult>`, `CommandResult` and `CommandError`. References no other project, and holds nothing else
- `Api` — ASP.NET Core app. Endpoints, request DTOs, request → value object mapping, `CommandResult` → HTTP mapping, startup (`Program.cs`)
- `Commands` — one command record and one handler per use case. Handlers load state, run the business checks, and append events or return a `CommandResult` error
- One file per use case in `Api` and `Commands`, named after the command and in matching folders: `Api/Keys/AddKey.cs` holds `AddKeyEndpoint` with its nested `AddKeyRequest` and a `MapAddKey` extension, `Commands/Keys/AddKey.cs` holds the `AddKey` record and `AddKeyHandler`. `Program.cs` maps every endpoint explicitly
- `Domain` — domain events (`Events/`), state model definitions and the tag vocabulary (`State/`), value objects (`Types/`)
  - State models are grouped into one static class per domain concept, named after it: `Tenant.NameTaken(...)`, `User.EmailTaken(...)`, `Subscription.Exists(...)`. A state model goes into the class of the thing it is keyed by, e.g. `Team.KeysAdded(teamId)`
  - A state model answers a question with a `bool` (`Tenant.IsNamed(tenantId, name)`, `Key.HasTranslation(...)`) or returns a domain type (`Team.Subscription` → `SubscriptionModelId?`). Comparisons belong in a state model, not in the handler. The one exception is the key limit, which spans two loads, so `KeyCount >= KeyLimit` stays in `AddKeyHandler`
  - Reducers turn stored values into domain types with the regular `From`. All stored events are assumed to have passed today's validation, so there is no separate unvalidated path
  - Handlers load all their state in one call and deconstruct the values with the context: `var (idTaken, emailTaken, context) = await loader.LoadAsync(User.Exists(...), User.EmailTaken(...))`
- `EventStore` — `IEventStore` and its PostgreSQL/Dapper implementation, `StateModel<T>`, `StateLoader`, the conditional append, schema setup, and the projection infrastructure every read model slice shares: `IProjection`, `ProjectionRunner<T>`, `AppendSignal`, `ReadModelDatabase`, `AddProjection<T>`
- `ReadModels/` (solution folder `ReadModels`) — one project per read model slice, each serving one kind of consumer, e.g. `PublicTranslations` for client applications. A slice owns one PostgreSQL schema named after it (`public_translations`), holds its `IProjection`, its query and result records and its internal query handlers, and registers each handler under `IQueryHandler<TQuery, TResult>` in an `Add<Slice>()` extension. `ProjectionRunner<T>` keeps each projection up to date in the background, woken by `PostgresEventStore` after every append. Raise a projection's `Version` whenever its tables or its logic change, so the slice is rebuilt from the first event
- `Commands.Tests` — handler and value object tests against an in-memory `IEventStore`
- `ReadModels/<Slice>.Tests` — a slice's projection and query tests against a throwaway database on the PostgreSQL server in `CCC_TEST_POSTGRES` (defaults to the local development server)
- `Architecture.Tests` — checks the dependency rules below
- `Api/Api.http` — smoke test with one request per endpoint and error path, including the expected status and body

### Dependency Rules
- `Api` → `Commands` → `Domain` → `EventStore`, and `Api` → each read model slice → `Domain`. `Api`, `Commands` and every slice also reference `Abstractions`, which references nothing. References only point this way
- A read model slice never references `Commands`, ASP.NET Core or another slice. Query endpoints live in `Api` in a folder named after the slice (`Api/PublicTranslations/`), like the command endpoints. New slices go into `ReadModelSlices` in `Architecture.Tests`
- `EventStore` knows nothing about the domain
- `Commands` never references ASP.NET Core; handlers return `CommandResult`, never `IResult`
- Command and query handlers are internal. Endpoints inject `ICommandHandler<TCommand>` or `IQueryHandler<TQuery, TResult>`, never a handler class
- Endpoints never touch the state models in `CCC.Domain.State`, `Tag` or the event store directly. Only `Program.cs` references `EventStore`, to register it
- Value objects check format and throw `InvalidValueException`, which the API turns into a 400. Handlers check rules that need the event log
- Every append in `PostgresEventStore` holds one advisory lock until commit, so positions become visible strictly in order and projection bookmarks never skip an event. Don't append outside that lock
- Event records keep primitive types, and must never be renamed: the stored event type is the record's class name

## Build & Run
```bash
dotnet build backend/CCC.sln
dotnet test backend/CCC.sln    # read model slice tests need PostgreSQL
dotnet run --project backend/Api/Api.csproj
```

## Tech Stack
- .NET 10.0
- ASP.NET Core
- React (frontend, separate project)

## Naming Conventions
- **Never use CRUD terminology** in request, command, or event names (no `Create`, `Read`, `Update`, `Delete`), and avoid it wherever else the domain is concerned, such as methods on domain types: value objects are built with `From(...)`, not `Create(...)`
- Use intent-revealing, domain language instead — e.g. `AddUser`, `RegisterUser`, `ArchiveProject`, `PublishTranslation`

## Code Comments
- Keep comments to a minimum. Only comment code that would otherwise be very confusing, such as a non-obvious concurrency guarantee, a workaround for library behaviour, or a constraint that breaks stored data if changed
- Don't restate what the code, a name or a test name already says. No template, section or banner comments

## Architectural Decisions
If and only if architectural context is needed, consult the `docs/architecture/` folder for ADRs (Architecture Decision Records) and other architectural conventions.
