# TODO: Read Model Slices

## Overview

Add read model projectors called **slices**. Each slice is a `.csproj`, a background event processor,
and a service provider for query concerns. Slices maintain their own read models and expose query
services that API endpoints use to answer client queries.

**Core rules:**
- Slices are notified via PostgreSQL LISTEN/NOTIFY — the notification carries no event data
- Each slice independently queries the event store to find what is new for it
- Each slice with persistent read models records the last global event position it has processed; on restart it resumes from that position without reprocessing earlier events
- Slices catch up from backlog in the background; the API accepts traffic immediately on startup
- Slices expose query services that API endpoints inject and call
- The write path (CommandContextBuilder + EventStore) is unchanged
- Read models are stored in MongoDB; all slices share one MongoDB instance and are separated by MongoDB database (one database per slice)

---

## Solution Structure (target)

```
CCC.sln
backend/
├── CCC/                          existing API project
├── CCC.Events/                   NEW: all domain event record types (shared)
├── CCC.EventStore/               NEW: extracted shared event store infrastructure
├── CCC.SliceRuntime/             NEW: shared slice base classes and hosting
└── Slices/
    ├── CCC.Slices.Organizations/ NEW: orgs, teams, subscription models
    ├── CCC.Slices.Users/         NEW: user registry
    └── CCC.Slices.Keys/          NEW: keys, translations, reference texts
```

**Dependency graph:**
```
CCC  ──────────────────────────────► CCC.Events
CCC  ──────────────────────────────► CCC.EventStore
CCC.EventStore  ────────────────────► (no domain dependencies)
CCC.SliceRuntime  ──────────────────► CCC.EventStore
CCC.Slices.*  ──────────────────────► CCC.SliceRuntime
CCC.Slices.*  ──────────────────────► CCC.Events
CCC  ──────────────────────────────► CCC.Slices.*  (for query services)
```

---

---

## Phase 1 — Create `CCC.Events`

- [ ] Create `backend/CCC.Events/CCC.Events.csproj` (class library, .NET 8)
- [ ] Add `CCC.Events` to `CCC.sln`
- [ ] Move all event record types from their domain modules in `CCC/` into `CCC.Events/`, grouped by domain:
  - `Organizations/` — `OrganizationAdded`, `TeamAdded`, `OrganizationOwnerAssigned`
  - `Projects/` — `ProjectAdded`, `LanguageAdded`
  - `Keys/` — `KeyAdded`, `KeyRenamed`, `ReferenceTextProvided`, `TranslationProvided`
  - `Users/` — `UserAdded`
  - `SubscriptionModels/` — `SubscriptionModelAdded`
- [ ] Add `CCC.Events` reference to `CCC.csproj`; fix all namespaces

---

## Phase 2 — Extract `CCC.EventStore`



- [ ] Create `backend/CCC.EventStore/CCC.EventStore.csproj` (class library, .NET 8)
- [ ] Move all files from `CCC/Infrastructure/EventStore/` into `CCC.EventStore/`
  - `EventStore.cs`, `EventRecord.cs`, `EventStoreSchema.cs`
  - `EventFilter.cs`, `EventFilterBuilder.cs`, `TypeFilterBuilder.cs`
  - `CommandContext.cs`, `CommandContextBuilder.cs`
  - `ConcurrencyException.cs`, `DateTimeOffsetTypeHandler.cs`
- [ ] Add `CCC.EventStore` reference to `CCC.csproj`; fix all namespaces
- [ ] Add `CCC.EventStore` to `CCC.sln`
- [ ] **Add NOTIFY to `EventStore.AppendAsync`**: after each successful INSERT, run
      `SELECT pg_notify('ccc_events_appended', '');`
      (no payload — slices must query the event store themselves)

---

## Phase 3 — Create `CCC.SliceRuntime`

- [ ] Create `backend/CCC.SliceRuntime/CCC.SliceRuntime.csproj` (class library, .NET 8)
- [ ] Add `CCC.SliceRuntime` to `CCC.sln`
- [ ] Reference `CCC.EventStore` from `CCC.SliceRuntime`
- [ ] Add `MongoDB.Driver` NuGet package to `CCC.SliceRuntime`

### MongoDB conventions

All slices connect to the same MongoDB instance (connection string from config: `ConnectionStrings:SliceStore`).
Each slice uses its own MongoDB **database**, named after the slice (e.g., `ccc_slice_keys`, `ccc_slice_orgs`, `ccc_slice_users`).
Within each database, data is organized into collections. There are no cross-slice database references.

### Types to implement

**`ISlice` interface**
```
string Name { get; }
Task ProcessNewEventsAsync(CancellationToken ct)
```

**`SliceBase` (abstract)**
- Constructor takes the event store connection string and a `IMongoDatabase` (injected, scoped to this slice's database)
- On startup: ensures a `slice_position` collection exists with an upsert-on-first-use pattern
  ```
  collection: slice_position
  document:   { _id: "<slice_name>", last_position: -1 }
  ```
- `ProcessNewEventsAsync`: reads `last_position` from `slice_position` (defaults to -1 on first run),
  calls `EventStore.ReadAsync(fromPosition: lastPosition + 1)`, calls `ProcessEventAsync(record)` for
  each new event in order, then **immediately upserts the updated position after each event** so that a
  crash mid-batch resumes from the last successfully written position rather than from the beginning
- `abstract Task ProcessEventAsync(EventRecord record)` — implemented by each concrete slice

**`EventStoreNotificationListener` (`IHostedService`)**
- Opens a dedicated Npgsql connection and runs `LISTEN ccc_events_appended`
- On notification: calls `ProcessNewEventsAsync()` on all registered `ISlice` instances
- Slices process independently (each has its own position, no shared state)
- On startup: triggers one initial `ProcessNewEventsAsync()` on all slices to begin catching up from backlog;
  this runs in the background — the API is already live and accepting traffic at this point

**`SliceServiceCollectionExtensions`**
- `AddSlice<T>()` extension on `IServiceCollection`
  - Registers `T` as singleton
  - Registers `T` as `ISlice`
  - Registers `EventStoreNotificationListener` as `IHostedService` (once, idempotent)

---

## Phase 4 — Implement `CCC.Slices.Keys`

- [ ] Create `backend/Slices/CCC.Slices.Keys/CCC.Slices.Keys.csproj`
- [ ] Reference `CCC.Events`, `CCC.EventStore`, and `CCC.SliceRuntime`
- [ ] Add to `CCC.sln`

### MongoDB database: `ccc_slice_keys`

Collections and their documents:

```
projects        — { _id, name, teamId, referenceLanguage }
languages       — { _id: { code, projectId }, parentCode }
keys            — { _id, name, projectId }
translations    — { _id: { keyId, languageCode }, text }
reference_texts — { _id: keyId, text }
```

### Events to process

`ProjectAdded`, `LanguageAdded`, `KeyAdded`, `KeyRenamed`, `ReferenceTextProvided`, `TranslationProvided`

### Query service interface

```csharp
interface IKeysQueryService
{
    Task<IReadOnlyList<KeyView>> GetKeysForProjectAsync(Guid projectId);
    Task<KeyView?> GetKeyAsync(Guid keyId);
    Task<TranslationView?> GetTranslationAsync(Guid keyId, string languageCode);
    Task<IReadOnlyList<TranslationView>> GetTranslationsForKeyAsync(Guid keyId);
    Task<int> CountKeysForTeamAsync(Guid teamId);
}
```

- [ ] Implement `KeysSlice : SliceBase` with `ProcessEventAsync` dispatching to per-event handlers
- [ ] Implement `KeysQueryService : IKeysQueryService` querying the slice DB
- [ ] Register both in DI via `AddSlice<KeysSlice>()` and `AddSingleton<IKeysQueryService, KeysQueryService>()`

---

## Phase 5 — Implement `CCC.Slices.Organizations`

- [ ] Create `backend/Slices/CCC.Slices.Organizations/CCC.Slices.Organizations.csproj`
- [ ] Reference `CCC.Events`, `CCC.EventStore`, and `CCC.SliceRuntime`
- [ ] Add to `CCC.sln`

### MongoDB database: `ccc_slice_organizations`

Collections and their documents:

```
organizations        — { _id, name }
subscription_models  — { _id, name, keyLimit }
teams                — { _id, name, organizationId, subscriptionModelId }
organization_owners  — { _id: { userId, organizationId } }
```

### Events to process

`OrganizationAdded`, `SubscriptionModelAdded`, `TeamAdded`, `OrganizationOwnerAssigned`

### Query service interface

```csharp
interface IOrganizationsQueryService
{
    Task<OrganizationView?> GetOrganizationAsync(Guid id);
    Task<IReadOnlyList<OrganizationView>> GetAllOrganizationsAsync();
    Task<TeamView?> GetTeamAsync(Guid id);
    Task<SubscriptionModelView?> GetSubscriptionModelAsync(Guid id);
    Task<IReadOnlyList<SubscriptionModelView>> GetAllSubscriptionModelsAsync();
}
```

---

## Phase 6 — Implement `CCC.Slices.Users`

- [ ] Create `backend/Slices/CCC.Slices.Users/CCC.Slices.Users.csproj`
- [ ] Reference `CCC.Events`, `CCC.EventStore`, and `CCC.SliceRuntime`
- [ ] Add to `CCC.sln`

### MongoDB database: `ccc_slice_users`

Collections and their documents:

```
users — { _id, name, email }
```

### Events to process

`UserAdded`

### Query service interface

```csharp
interface IUsersQueryService
{
    Task<UserView?> GetUserAsync(Guid id);
    Task<UserView?> GetUserByEmailAsync(string email);
    Task<IReadOnlyList<UserView>> GetAllUsersAsync();
}
```

---

## Phase 7 — Wire slices into the API

- [ ] Add project references from `CCC` to each slice project
- [ ] Register slices in `Program.cs` using `AddSlice<T>()`
- [ ] Add GET endpoints to `KeyEndpoints`, `OrganizationEndpoints`, `UserEndpoints`
      that inject and call slice query services
- [ ] Verify end-to-end: write an event via POST, confirm slice updates, confirm GET returns updated data

---

## Key Design Decisions (recorded)

| Decision | Choice | Reason |
|---|---|---|
| Event record types | Separate `CCC.Events` library | Slices need to deserialize events without depending on the API project; shared library breaks the cycle |
| Notification mechanism | PostgreSQL LISTEN/NOTIFY | Already using PG; no extra infrastructure; zero payload enforces correct design |
| Event data in notification | Never transferred | Avoids concurrency issues; each slice polls event store independently |
| Read model storage | MongoDB | Document model fits denormalized read models well; flexible schema per slice |
| MongoDB isolation | One database per slice within a shared MongoDB instance | Shared server = simple to operate; separate databases = no cross-slice coupling |
| Position tracking | `slice_position` collection in each slice's own MongoDB database | Co-located with the read models it tracks; reset/rebuild is self-contained |
| Position persistence granularity | Upsert position after every single event | A crash mid-batch resumes from last written position, not from the start |
| Startup behavior | API accepts traffic immediately; slices catch up in background | No artificial delay on startup; read models may be briefly stale after a restart, which is acceptable |
| Concurrency within a slice | Single-threaded sequential processing | Slices process their backlog serially; multiple slices run in parallel with each other |
| Slice granularity | One slice per major domain context | Matches existing module structure |
