# CCC — Command Context Consistency Example

CCC is an example application for illustrating Command Context Consistency (CCC).

It implements a very basic version of the ideas behind the [Calinga](https://www.calinga.io) system: tenants, projects, translation keys, and related write-side business rules.

The repository is not intended to be a generic content management system. Its main purpose is to explore and demonstrate an event-sourced write model based on minimal command-specific context slices and conditional appends.

## Repository Structure

- `backend/` - ASP.NET Core backend with the event-sourced write model and API, split into `Api`, one project per module under `Modules/`, `Domain`, `EventStore`, and the shared projects `Abstractions`, `Dcb`, `Projections` and `TestKit` under `Base/` (see ADR-006, ADR-009, ADR-011, ADR-012 and ADR-013)
- `frontend/` - React frontend for exploring and exercising the sample application
- `presentation/` - standalone React/RevealJS slide deck used to explain CCC in this repository
- `docs/architecture/` - architecture decision records for the main technical choices

---

## Quick Start

### Backend

```bash
dotnet build backend/CCC.sln
dotnet test backend/CCC.sln
dotnet run --project backend/Api/Api.csproj
```

The backend and the projection slice tests need PostgreSQL on `localhost:5432`. The tests use the server in the `CCC_TEST_POSTGRES` connection string instead, if it is set.

In development, the OpenAPI document is served at `http://localhost:5039/openapi/v1.json` and Swagger UI at `http://localhost:5039/swagger`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

UI is available at `http://localhost:5173`. Requires the backend to be running.

### Presentation

```bash
cd presentation
npm install
npm run dev
```

The browser-hosted presentation runs on Vite's default dev port unless that port is already in use.

---

## Further Reading

### Architecture

| Document | Description |
|----------|-------------|
| [ADR-001: Use Architecture Decision Records](docs/architecture/adr-001-adrs.md) | Establishes the ADR format and file naming convention used in this project |
| [ADR-002: Event Sourcing with Command-Context-Consistency](docs/architecture/adr-002-event-sourcing-ccc.md) | Defines the core architectural model used in this example |
| [ADR-003: PostgreSQL as Event Log Database](docs/architecture/adr-003-postgresql.md) | Explains why PostgreSQL is used as the append-only event log |
| [ADR-004: Dapper as Event Log Data Access Library](docs/architecture/adr-004-dapper-event-store.md) | Explains why the event store uses Dapper and explicit SQL |
| [ADR-005: No CRUD Terminology in Requests, Commands, and Events](docs/architecture/adr-005-no-crud-naming.md) | Defines the naming convention for commands, requests, and events |
| [ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects](docs/architecture/adr-006-project-structure.md) | Defines the backend projects, the direction of their dependencies, and where validation happens |
| [ADR-007: Projections Answer Queries, One Project per Slice](docs/architecture/adr-007-projections.md) | Defines how projections are built, woken up and queried, and why appends are serialized |
| [ADR-008: An Abstractions Project for the Handler Contracts](docs/architecture/adr-008-abstractions.md) | Puts the command and query handler contracts into a project that references nothing |
| [ADR-009: Modules by Area of Work](docs/architecture/adr-009-modules.md) | Groups commands, projections and queries into one project per area of work |
| [ADR-010: Projections Stall After Repeated Failures](docs/architecture/adr-010-stalled-projections.md) | Defines when a projection gives up, how the stall is recorded, and how to unstall it |
| [ADR-011: Projection Infrastructure in a ProjectionBase Project](docs/architecture/adr-011-projection-base.md) | Moves the projection runner and its helpers out of the event store |
| [ADR-012: State Model Primitives in a Dependency-Free Dcb Project](docs/architecture/adr-012-dcb.md) | Moves state models, tags and event records into a project that references nothing, so the domain no longer depends on the event store |
| [ADR-013: A Base Folder for the Shared Projects](docs/architecture/adr-013-base-folder.md) | Groups `Abstractions`, `Dcb`, `Projections` and `TestKit` under `Base/`, with `CCC.Base.*` namespaces |
