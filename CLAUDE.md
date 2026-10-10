# CCC - Multi-Tenant CMS (Content/Copy Management System)

## Purpose
CCC is a demo project. It shows how to build a .NET backend on an event store with dynamic consistency boundaries (DCB), with commands and queries separated (CQS) and read models projected from the event log (CQRS). The frontend is secondary: it only gives access to the commands and queries.

## Demo Domain
A multi-tenant CMS in which users define keys and translations for those keys, such as label texts for user controls in different languages. Keys can also represent welcome texts, mail templates, or any other static data consumed by a target application.

## Architecture
- **Backend (C# / ASP.NET Core)** — the actual subject of the demo, with two API surfaces:
  1. **Internal API** — the commands and queries the frontend uses to manage tenants, teams, projects, keys and translations
  2. **Public API** — lets client applications fetch translations by project and locale
- **Frontend (React)** — a thin UI over the internal API

## Project Structure and Dependency Rules
Read these files whenever you need to explore the project structure, and always before modifying code:

- [docs/project-structure.md](docs/project-structure.md) — the projects, what goes where, and the conventions inside each project
- [docs/dependency-rules.md](docs/dependency-rules.md) — which project may reference which, and the rules `Architecture.Tests` enforces

## Build & Run
```bash
dotnet build backend/CCC.sln
dotnet test backend/CCC.sln    # tests of modules with read models need PostgreSQL
dotnet run --project backend/Api/Api.csproj
```

## Tech Stack
- .NET 10.0
- ASP.NET Core
- PostgreSQL with Dapper
- React (frontend, separate project)

## Naming Conventions
- **Never use CRUD terminology** in request, command, or event names (no `Create`, `Read`, `Update`, `Delete`), and avoid it wherever else the domain is concerned, such as methods on domain types: value objects are built with `From(...)`, not `Create(...)`
- Use intent-revealing, domain language instead — e.g. `AddUser`, `RegisterUser`, `ArchiveProject`, `PublishTranslation`

## Code Comments
- Keep comments to a minimum. Only comment code that would otherwise be very confusing, such as a non-obvious concurrency guarantee, a workaround for library behaviour, or a constraint that breaks stored data if changed
- Don't restate what the code, a name or a test name already says. No template, section or banner comments

## Architectural Decisions
If and only if architectural context is needed, consult the `docs/architecture/` folder for ADRs (Architecture Decision Records) and other architectural conventions.
