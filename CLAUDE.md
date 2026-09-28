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

- `Api` — ASP.NET Core app. Endpoints, request DTOs, request → value object mapping, `CommandResult` → HTTP mapping, startup (`Program.cs`)
- `Commands` — one command record and one handler per use case. Handlers load state, run the business checks, and append events or return a `CommandResult` error
- `Domain` — domain events (`Events/`), state model definitions and the tag vocabulary (`State/`), value objects (`Types/`)
  - State models are grouped into one static class per domain concept, named after it: `Tenant.NameTaken(...)`, `User.EmailTaken(...)`, `Subscription.Exists(...)`. A state model goes into the class of the thing it is keyed by, e.g. `Team.KeyCount(teamId)`
- `EventStore` — `IEventStore` and its PostgreSQL/Dapper implementation, `StateModel<T>`, `StateLoader`, the conditional append, schema setup
- `Commands.Tests` — handler and value object tests against an in-memory `IEventStore`
- `Architecture.Tests` — checks the dependency rules below
- `Api/Api.http` — smoke test with one request per endpoint and error path, including the expected status and body

### Dependency Rules
- `Api` → `Commands` → `Domain` → `EventStore`. References only point this way
- `EventStore` knows nothing about the domain
- `Commands` never references ASP.NET Core; handlers return `CommandResult`, never `IResult`
- Endpoints never touch the state models in `CCC.Domain.State`, `Tag` or the event store directly. Only `Program.cs` references `EventStore`, to register it
- Value objects check format and throw `InvalidValueException`, which the API turns into a 400. Handlers check rules that need the event log
- Event records keep primitive types, and must never be renamed: the stored event type is the record's class name

## Build & Run
```bash
dotnet build backend/CCC.sln
dotnet test backend/CCC.sln
dotnet run --project backend/Api/Api.csproj
```

## Tech Stack
- .NET 10.0
- ASP.NET Core
- React (frontend, separate project)

## Naming Conventions
- **Never use CRUD terminology** in request, command, or event names (no `Create`, `Read`, `Update`, `Delete`)
- Use intent-revealing, domain language instead — e.g. `AddUser`, `RegisterUser`, `ArchiveProject`, `PublishTranslation`

## Code Comments
- Keep comments to a minimum. Only comment code that would otherwise be very confusing, such as a non-obvious concurrency guarantee, a workaround for library behaviour, or a constraint that breaks stored data if changed
- Don't restate what the code, a name or a test name already says. No template, section or banner comments

## Architectural Decisions
If and only if architectural context is needed, consult the `docs/architecture/` folder for ADRs (Architecture Decision Records) and other architectural conventions.
