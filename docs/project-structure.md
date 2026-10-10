# Project Structure

All backend projects live in `backend/`, share `backend/CCC.sln`, and inherit `net10.0`, `Nullable` and `ImplicitUsings` from `backend/Directory.Build.props`. Project names have no `CCC.` prefix, but each sets `RootNamespace`, so namespaces keep it: the `Domain` project's code lives in `CCC.Domain.*`.

The rules for how these projects may reference each other are in [dependency-rules.md](dependency-rules.md).

- `Abstractions` — the contracts every module shares at its boundary to `Api`: `ICommandHandler<TCommand>`, `IQueryHandler<TQuery, TResult>`, `CommandResult` and `CommandError`. References no other project, and holds nothing else
- `Api` — ASP.NET Core app. Endpoints, request DTOs, request → value object mapping, `CommandResult` → HTTP mapping, startup (`Program.cs`)
- `Modules/` (solution folder `Modules`) — one project per module, each covering one area of work (ADR-009): `AppAdministration`, `TenantAdministration`, `TeamAdministration`, `TranslationEditor` and `TranslationDelivery`. A module holds the commands, command handlers, projections and query handlers of its area of work, and registers its handlers in an `Add<Module>()` extension. A use case goes into the module whose tasks it changes together with and whose queries it needs; who usually performs it is only a hint
  - Command handlers load state, run the business checks, and append events or return a `CommandResult` error
  - A module with read models owns one PostgreSQL schema named after it (`translation_delivery`) and its `IProjection`s. `ProjectionRunner<T>` keeps each projection up to date in the background, woken by `PostgresEventStore` after every append. Raise a projection's `Version` whenever its tables or its logic change, so the module's schema is rebuilt from the first event
- One file per use case in `Api` and in the module, named after the command or query and in folders named after the module: `Api/TranslationEditor/AddKey.cs` holds `AddKeyEndpoint` with its nested `AddKeyRequest` and a `MapAddKey` extension, `Modules/TranslationEditor/AddKey.cs` holds the `AddKey` record and `AddKeyHandler`. `Program.cs` calls every `Add<Module>()` and maps every endpoint explicitly
- `Domain` — the shared kernel every module builds on: domain events (`Events/`), state model definitions and the tag vocabulary (`State/`), value objects (`Types/`)
  - State models are grouped into one static class per domain concept, named after it: `Tenant.NameTaken(...)`, `User.EmailTaken(...)`, `Subscription.Exists(...)`. A state model goes into the class of the thing it is keyed by, e.g. `Team.KeysAdded(teamId)`
  - A state model answers a question with a `bool` (`Tenant.IsNamed(tenantId, name)`, `Key.HasTranslation(...)`) or returns a domain type (`Team.Subscription` → `SubscriptionModelId?`). Comparisons belong in a state model, not in the handler. The one exception is the key limit, which spans two loads, so `KeyCount >= KeyLimit` stays in `AddKeyHandler`
  - Reducers turn stored values into domain types with the regular `From`. All stored events are assumed to have passed today's validation, so there is no separate unvalidated path
  - Handlers load all their state in one call and deconstruct the values with the context: `var (idTaken, emailTaken, context) = await loader.LoadAsync(User.Exists(...), User.EmailTaken(...))`
- `EventStore` — `IEventStore` and its PostgreSQL/Dapper implementation, `StateModel<T>`, `StateLoader`, `StateContext.TryAppendAsync` (the conditional append), schema setup, and the projection infrastructure every module with read models shares: `IProjection`, `ProjectionRunner<T>`, `AppendSignal`, `ReadModelDatabase`, `AddProjection<T>`
- `Modules/<Module>.Tests` — a module's tests. Handler tests run against the in-memory `IEventStore` from `TestKit`; projection and query tests run against a throwaway database on the PostgreSQL server in `CCC_TEST_POSTGRES` (defaults to the local development server)
- `TestKit` — what every handler test project shares: `InMemoryEventStore`, `CommandFixture` with its `Given…` setup helpers, and `CommandAssert`. The fixture registers every module, so tests set up state through other modules' commands and get the same events and tags as production
- `Domain.Tests` — value object tests
- `Architecture.Tests` — checks the rules in [dependency-rules.md](dependency-rules.md)
- `Api/Api.http` — smoke test with one request per endpoint and error path, including the expected status and body
