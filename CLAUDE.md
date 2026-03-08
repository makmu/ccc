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
- Solution file: `CCC.sln`
- Main project: `CCC/CCC.csproj`
- Entry point: `CCC/Program.cs`

## Build & Run
```bash
dotnet build CCC.sln
dotnet run --project CCC/CCC.csproj
```

## Tech Stack
- .NET 8.0
- ASP.NET Core
- React (frontend, separate project)

## Architectural Decisions
If and only if architectural context is needed, consult the `docs/architecture/` folder for ADRs (Architecture Decision Records) and other architectural conventions.
