# CCC — Command Context Consistency Example

CCC is an example application for illustrating Command Context Consistency (CCC).

It implements a very basic version of the ideas behind the [Calinga](https://www.calinga.io) system: tenants, projects, translation keys, and related write-side business rules.

The repository is not intended to be a generic content management system. Its main purpose is to explore and demonstrate an event-sourced write model based on minimal command-specific context slices and conditional appends.

## Repository Structure

- `backend/` - ASP.NET Core backend with the event-sourced write model and API
- `frontend/` - React frontend for exploring and exercising the sample application
- `presentation/` - standalone React/RevealJS slide deck used to explain CCC in this repository
- `docs/architecture/` - architecture decision records for the main technical choices

---

## Quick Start

### Backend

```bash
dotnet build backend/CCC.sln
dotnet run --project backend/CCC/CCC.csproj
```

Swagger UI is available at `http://localhost:5039/swagger` in development.

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
