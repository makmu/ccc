# Refactoring plan: split the backend into API, Domain, Commands and EventStore

## Goal

Today everything lives in `backend/CCC`. Each endpoint loads its own state, runs the business checks, appends events and builds the HTTP response. This plan splits that work across four projects, one job each.

| Project | Kind | Contains |
|---|---|---|
| `CCC.Api` | ASP.NET Core web app | Endpoints, request DTOs, mapping requests to value objects, mapping command results to HTTP, DI and startup |
| `CCC.Domain` | Class library | Domain events, domain state with the state model definitions and the tag vocabulary, value objects |
| `CCC.Commands` | Class library | One command record and one handler per use case. A handler takes value objects, loads state and runs the business checks. Then it appends events or returns an error |
| `CCC.EventStore` | Class library | The `IEventStore` interface and its PostgreSQL/Dapper implementation, reading and writing events, state loading, the conditional append, schema setup |

## Target structure

```
backend/
├── CCC.sln
├── CCC.Api/
│   ├── Keys/            KeyEndpoints.cs, *Request.cs
│   ├── Projects/
│   ├── SubscriptionModels/
│   ├── Tenants/
│   ├── Users/
│   ├── CommandResultExtensions.cs   maps CommandResult to IResult
│   ├── InvalidValueFilter.cs        turns InvalidValueException into a 400
│   ├── CCC.Api.http                 smoke test with expected responses
│   └── Program.cs
├── CCC.Domain/
│   ├── Events/          TenantRegistered.cs, KeyAdded.cs, ...
│   ├── State/           State.cs, Tag.cs
│   └── ValueObjects/    TenantName.cs, KeyName.cs, LanguageCode.cs, InvalidValueException.cs, ...
├── CCC.Commands/
│   ├── CommandResult.cs, CommandError.cs, ICommandHandler.cs
│   ├── StateContextExtensions.cs   TryAppendAsync
│   ├── Tenants/         RegisterTenant.cs, RegisterTenantHandler.cs, ...
│   ├── Projects/
│   ├── Keys/
│   ├── SubscriptionModels/
│   ├── Users/
│   └── ServiceCollectionExtensions.cs   AddCommandHandlers
├── CCC.EventStore/
│   ├── IEventStore.cs, PostgresEventStore.cs, EventStoreSchema.cs
│   ├── EventRecord.cs, EventFilter.cs, Tags.cs, ConcurrencyException.cs
│   ├── StateModel.cs, IStateModel.cs, StateLoader.cs, StateContext.cs
│   ├── IStateLoader.cs, DateTimeOffsetTypeHandler.cs, StringArrayTypeHandler.cs
│   └── ServiceCollectionExtensions.cs   AddEventStore
├── CCC.Commands.Tests/      handler and value object tests, in-memory IEventStore
├── CCC.Architecture.Tests/  dependency rule tests
└── Directory.Build.props    net10.0, Nullable, ImplicitUsings for every project
```

## Dependency graph

```
CCC.Api ──────► CCC.Commands ──────► CCC.Domain ──────► CCC.EventStore
   │                 │                                        ▲
   │                 └────────────────────────────────────────┤
   └──────────────────────────────────────────────────────────┘
```

The API also references `CCC.Domain` directly, because it builds the value objects.

Rules:
- `CCC.EventStore` references no other CCC project. It knows nothing about tenants, keys or any other domain concept.
- `CCC.Domain` references only `CCC.EventStore`. It needs `StateModel<T>` to define its state, see decision D1.
- `CCC.Commands` never references ASP.NET Core. Handlers return a `CommandResult`, never an `IResult`.
- `CCC.Api` holds no business rules. It parses input, sends commands and turns results into HTTP responses.

## What must not change

Stored events and the HTTP contract have to be identical before and after. Treat each point below as a regression check. Most of them don't fail loudly when broken. Existing events just stop matching, and the state models quietly return wrong answers.

- **Event type names.** `StateContext.AppendAsync` stores `typeof(T).Name`, and `StateModel.When<TEvent>` finds events by that name. Moving a record to another namespace is safe. Renaming it orphans every stored event of that type.
- **Payload shape.** Payloads are JSON-serialized records, so property names and types stay as they are. See decision D2.
- **Tags.** The `Tag.*` keys such as `tenant-id` and `tenant-name` must produce the same strings, and so must the name canonicalization, `Trim().ToLowerInvariant()`.
- **Order of checks.** Each endpoint checks its conditions in a fixed order, and that order decides which error the caller sees. Handlers keep it. That includes the early no-op returns for an unchanged reference text or translation and for a tenant renamed to its current name in different casing.
- **One snapshot per decision.** Each endpoint loads all its state models with a single `StateLoader.LoadAsync` call and appends through that call's `StateContext`. `AddKey` loads `KeyLimit` a second time, outside the append condition, and that's deliberate. Keep it.
- **HTTP responses.** Every route returns the same status codes and the same messages.

---

## Phase 0: Preparation

- [x] Create the branch `refactor/project-split`
- [x] Add `backend/Directory.Build.props` with `TargetFramework` set to `net10.0` and `Nullable` and `ImplicitUsings` enabled. Remove those three settings from `CCC.csproj`. Every new project then picks up .NET 10 automatically, and a later runtime bump is a one-line change
- [x] Bump `Microsoft.AspNetCore.OpenApi` from 8.0.14 to the current 10.x release so it matches the runtime. Done with 10.0.12. `Swashbuckle.AspNetCore` 6.6.2 crashed at startup against the new Microsoft.OpenApi, so it's now 10.2.3
- [x] Build `backend/CCC.sln` and run a smoke test with one request per endpoint, through Swagger or a `.http` file. Save the responses. Every later phase gets compared against them. The expected status and body now live in `backend/CCC/CCC.http`, recorded against an empty `ccc_baseline` database
- [x] Optionally commit that file as `backend/CCC/CCC.http`. Repeating the smoke test by hand after every phase gets old fast

## Phase 1: Extract `CCC.EventStore`

- [x] Create `backend/CCC.EventStore/CCC.EventStore.csproj`, a class library. It inherits `net10.0`, `Nullable` and `ImplicitUsings` from `Directory.Build.props`
- [x] Move the `Dapper` and `Npgsql` package references from `CCC.csproj` into the new project
- [x] Move every file in `CCC/Infrastructure/EventStore/` into `CCC.EventStore/`:
  - `EventStore.cs`, `EventRecord.cs`, `EventFilter.cs`, `EventStoreSchema.cs`, `Tags.cs`
  - `StateModel.cs`, `IStateModel.cs`, `StateLoader.cs`
  - `CommandContext.cs`, renamed to `StateContext.cs` so the file name matches its class
  - `ConcurrencyException.cs`, `DateTimeOffsetTypeHandler.cs`, `StringArrayTypeHandler.cs`
- [x] Change the namespace from `CCC.Infrastructure.EventStore` to `CCC.EventStore`
- [x] Make public the types other projects need. Everything is internal today. That covers `StateModel`, `StateModel<T>`, `IStateModel`, `StateLoader`, `StateContext`, `Tags`, `ConcurrencyException`, `EventFilter`, `EventRecord` and `EventStoreSchema`. The Dapper type handlers and `PostgresEventStore` stay internal. `EventFilter.Entry` had to become public, because the public `EventFilter` takes a list of entries in its constructor
- [x] Add an `IEventStore` interface with both `AppendAsync` overloads, `LoadAsync(EventFilter)` and `ReadAsync(fromPosition, types)`
- [x] Rename the class `EventStore` to `PostgresEventStore` and have it implement `IEventStore`. The rename also ends the clash between the class name and its namespace
- [x] Change `StateLoader` and `StateContext` to depend on `IEventStore` instead of the concrete class
- [x] Add an `IStateLoader` interface with `LoadAsync(params IStateModel[])`. Handlers will depend on it, so tests can swap in a fake
- [x] Add `AddEventStore(this IServiceCollection, string connectionString)` in `ServiceCollectionExtensions`. It registers the Dapper type handlers, which move here from `Program.cs`. It also registers `PostgresEventStore` as the singleton `IEventStore` and `StateLoader` as the transient `IStateLoader`. This needs the `Microsoft.Extensions.DependencyInjection.Abstractions` package
- [x] Make `EventStoreSchema.CreateAsync` public so the API can still call it at startup
- [x] Add `CCC.EventStore` to `CCC.sln` and reference it from `CCC.csproj`
- [x] Update `Program.cs` and all endpoints to the new namespace and registrations
- [x] Delete the empty `CCC/Infrastructure/` folder
- [x] Build and rerun the smoke test. The results must match the baseline

## Phase 2: Create `CCC.Domain` with events and state

- [x] Create `backend/CCC.Domain/CCC.Domain.csproj`, a `net10.0` class library that references `CCC.EventStore`
- [x] Add it to `CCC.sln` and reference it from `CCC.csproj`
- [x] Move `CCC/Domain/Events/*.cs` to `CCC.Domain/Events/` under the namespace `CCC.Domain.Events`, and make the records public. Don't rename any of them. See "What must not change"
- [x] Move `State.cs` and `Tag.cs` to `CCC.Domain/State/` under `CCC.Domain.State`, as `public static` classes
- [x] Move the `CanonicalName()` extension from `CCC/Helpers/StringExtensions.cs` into `CCC.Domain` and delete `CCC/Helpers/`. It only lives there until phase 3 replaces it
- [x] Delete the old `CCC/Domain/` folder
- [x] Build and rerun the smoke test

## Phase 3: Add value objects

A value object checks format. If the input is malformed, the value object can't be constructed. Business rules like uniqueness, existence and limits need the event log, so they stay in the handlers.

- [x] Pick one construction pattern and use it for every value object, for example `static Result<TenantName> Create(string raw)` or `bool TryCreate(string raw, out TenantName value, out string error)`. Either works. Mixing them is worse than both. Implement the types as `readonly record struct` or `sealed record`. Chosen: a third option, `static TenantName Create(string? raw)` that throws `InvalidValueException`. `InvalidValueFilter` in the API turns it into a 400 for every endpoint, so endpoints need no check per field. All types are `readonly record struct`
- [x] ID value objects wrap a `Guid` and reject `Guid.Empty`. They are `TenantId`, `TeamId`, `ProjectId`, `KeyId`, `UserId` and `SubscriptionModelId`
- [x] Name value objects reject empty and whitespace-only input. Each exposes `Value`, the name as entered, and `Canonical`, the trimmed lowercase form that replaces `CanonicalName()`. They are `TenantName`, `TeamName` and `ProjectName`
- [x] `KeyName` rejects empty input. The code doesn't canonicalize key names today, since `Tag.KeyName(name)` takes the raw name. Keep it that way. Changing it changes behaviour and deserves its own discussion
- [x] `LanguageCode` rejects empty input. A BCP 47 format check would be nice but isn't required
- [x] `EmailAddress` rejects empty input and does a basic format check. The code tags emails as entered today, so keep them raw
- [x] `KeyLimit` is an integer of 0 or more
- [x] Add `TranslationText` and `ReferenceText` wrappers only if they earn their keep. Both allow empty strings, so there's little to validate. Skipped, along with user names and subscription model names. None of them are tagged or checked, so they stay plain strings
- [x] Change the `Tag.*` factories to take value objects, e.g. `Tag.TenantName(TenantName name) => ("tenant-name", name.Canonical)`. After that nobody can pass an uncanonicalized name by accident. I think that's the best payoff in this whole phase
- [x] Change the `State.*` factories to take value objects too, e.g. `State.TenantNameTaken(TenantName name)`. The state they return stays primitive. A caller that needs a value object back, like the old name in a rename, converts it
- [x] Remove `CanonicalName()` once nothing calls it. It is now the internal `CanonicalForm.Of`, which the name value objects and `State.TenantNameTaken` share
- [x] Build, then check that every `Tag.*` factory still produces exactly the same string for the same input. Checked by running `CCC.http` against the phase 0 build and this one and diffing the stored events: type, payload and tags are identical

## Phase 4: Create `CCC.Commands`

### Shared pieces

- [x] Create `backend/CCC.Commands/CCC.Commands.csproj`, a `net10.0` class library that references `CCC.Domain` and `CCC.EventStore` and nothing from ASP.NET Core
- [x] Add it to `CCC.sln` and reference it from the API project
- [x] Add `CommandError(CommandErrorKind Kind, string Message)` with `CommandErrorKind { NotFound, Conflict, Invalid }`
- [x] Add `CommandResult`, which is either a success or a failure carrying a `CommandError`. Give it the helpers `CommandResult.Ok`, `.NotFound(msg)`, `.Conflict(msg)` and `.Invalid(msg)`
- [x] Add `ICommandHandler<TCommand>` with `Task<CommandResult> HandleAsync(TCommand command)`. The `CancellationToken` is left out, because neither `IStateLoader` nor `IEventStore` takes one yet
- [x] Add one helper for appending, `StateContext.TryAppendAsync`, that catches `ConcurrencyException` and returns `Conflict("A concurrent operation modified the event store. Please retry.")`. All 12 endpoints repeat that `try/catch` today
- [x] Add `AddCommandHandlers(this IServiceCollection)`, which registers each handler by name. Twelve lines don't need assembly scanning

### Commands and handlers

Each command is a record of value objects. Each handler takes over the logic of one endpoint. It builds the state models, loads them in one call, checks the conditions in today's order and appends the event with the same tags. Names follow ADR-005, so no Create, Read, Update or Delete.

| Command | Moved out of | Notes |
|---|---|---|
| [x] `AddSubscriptionModel` | `SubscriptionModelEndpoints`, `POST /subscription-models` | |
| [x] `AddUser` | `UserEndpoints`, `POST /users` | |
| [x] `RegisterTenant` | `TenantEndpoints`, `POST /tenants` | The check for at least one subscription model runs before loading and returns `Invalid` |
| [x] `RenameTenant` | `TenantEndpoints`, `POST /tenants/{id}/rename` | Keep the case-insensitive no-op. Tag both the old and the new canonical name |
| [x] `AddTeam` | `TenantEndpoints`, `POST /tenants/{id}/teams` | |
| [x] `AssignTenantOwner` | `TenantEndpoints`, `POST /tenants/{id}/owners` | |
| [x] `AddProject` | `ProjectEndpoints`, `POST /teams/{id}/projects` | |
| [x] `AddLanguage` | `ProjectEndpoints`, `POST /teams/{id}/projects/{id}/languages` | |
| [x] `AddKey` | `KeyEndpoints`, `POST .../keys` | Keep the second `KeyLimit` load outside the append condition |
| [x] `RenameKey` | `KeyEndpoints`, `POST .../keys/{id}/rename` | Tag both the old and the new key name |
| [x] `ProvideReferenceText` | `KeyEndpoints`, `POST .../keys/{id}/reference-text` | Returns `Ok` without appending when the text is unchanged |
| [x] `ProvideTranslation` | `KeyEndpoints`, `POST .../keys/{id}/translations` | Returns `Ok` without appending when the text is unchanged |

- [x] Copy every error message string exactly
- [x] Build. Nothing calls the handlers yet, so this phase has no smoke test

## Phase 5: Route the API through the handlers and rename it to `CCC.Api`

- [x] Rename `backend/CCC/` to `backend/CCC.Api/` and `CCC.csproj` to `CCC.Api.csproj` with `git mv`, which keeps the history. The endpoint namespaces are already `CCC.Api.*`, so they stay. `CCC.http` moved along as `CCC.Api.http`
- [x] Update the project path in `CCC.sln`
- [x] Flatten `CCC.Api/Api/<Feature>/` to `CCC.Api/<Feature>/`
- [x] Add `ToHttpResult(this CommandResult)` in `CommandResultExtensions`. It maps `Ok` to 200, `NotFound` to 404, `Conflict` to 409 and `Invalid` to 400, with the error message as the body
- [x] Rewrite each endpoint to do four things:
  1. Bind the request DTO and route parameters.
  2. Turn them into value objects. Malformed input returns 400 with the error message.
  3. Build the command and call the injected `ICommandHandler<TCommand>`.
  4. Return `result.ToHttpResult()`.
- [x] Remove every use of `State`, `Tag`, `Tags`, `StateLoader` and `ConcurrencyException` from the endpoints. They shouldn't touch the event store at all
- [x] In `Program.cs`, replace the manual registrations with `AddEventStore(connectionString)` and `AddCommandHandlers()`. Keep the schema setup call at startup
- [x] Check that `Properties/launchSettings.json` and the `appsettings*.json` files moved with the project and still load
- [x] Build and rerun the smoke test. Every route must return the baseline status code and message. Also check that events written before the refactoring still read correctly, for example by renaming a tenant registered before phase 1. Checked by filling the database with the phase 0 build, then running requests against this build that depend on those events: an old rename, the old key count, an unchanged old translation

## Phase 6: Check the boundaries

- [x] Search `CCC.Api` for `using CCC.EventStore` and `using CCC.Domain.State`. Only `Program.cs` should reference the event store, and only to register it
- [x] Search `CCC.Commands` for `Microsoft.AspNetCore`. There should be no hits
- [x] Search `CCC.EventStore` for `CCC.Domain` and `CCC.Commands`. There should be no hits
- [x] Optionally add an architecture test with `NetArchTest.Rules`, so these rules don't depend on someone remembering to grep. Done as `backend/CCC.Architecture.Tests`. A deliberately added `IStateLoader` parameter in an endpoint makes it fail, naming the endpoint class

## Phase 7: Tests

I'd do this phase even though it's optional. Handlers that take value objects and an `IStateLoader` will be the easiest code in the repo to test, and right now there are no tests at all.

- [x] Create `backend/CCC.Commands.Tests` with xUnit
- [x] Write an in-memory `IEventStore` fake. It has to match the behaviour of `EventFilter.Matches` and the conditional append, or the tests prove nothing
- [x] Write given/when/then tests for each handler. Start from some existing events, run the command, then check the appended events or the returned error. The tests resolve handlers through `AddCommandHandlers`, the same way the API does, and mostly build their starting state by running earlier commands, so the setup carries the tags the handlers really write. `ConcurrencyTests` slips a competing write in between load and append
- [x] Unit-test the value objects, canonicalization included

## Phase 8: Documentation

- [x] Update `CLAUDE.md` with the new project structure, the new run command `dotnet run --project backend/CCC.Api/CCC.Api.csproj` and the dependency rules
- [x] Add `docs/architecture/adr-006-project-structure.md` covering the four-project split, the dependency direction and decisions D1 to D3
- [x] Update `README.md` if it mentions the old project path. Also added `dotnet test` to its quick start and ADR-006 to its ADR table

---

## Decisions

**D1. `StateModel<T>` lives in `CCC.EventStore`.** `StateModel<T>`, `IStateModel` and `Tags` contain nothing domain-specific, so they belong with the event store. That forces `CCC.Domain` to reference `CCC.EventStore`, which isn't pretty. The alternative is a small `CCC.EventStore.Abstractions` project for the domain to depend on instead. I don't think it's worth it yet. The event store has no domain dependencies, so the reference can't create a cycle.

**D2. Events keep primitive types.** Event properties stay `Guid` and `string`. The stored JSON doesn't change, and nobody has to write JSON converters. Value objects stop at the command boundary. Handlers unwrap them when they build events.

**D3. The API checks format and the handlers check business rules.** Value objects reject malformed input, so the API returns 400 before a command even exists. Handlers check the rules that need the event log, like existence, uniqueness and limits. They also run the few checks that come before loading, such as `RegisterTenant` requiring at least one subscription model.

## Commit sequence

Each commit should build and pass the smoke test.

1. `extract CCC.EventStore project with IEventStore`
2. `move events and state into CCC.Domain`
3. `introduce value objects`
4. `add CCC.Commands with command handlers`
5. `route API endpoints through command handlers; rename to CCC.Api`
6. `add command handler tests`, optional
7. `document project structure (ADR-006, CLAUDE.md)`
