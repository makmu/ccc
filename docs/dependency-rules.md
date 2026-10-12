# Dependency Rules

What each project holds is described in [project-structure.md](project-structure.md). `Architecture.Tests` checks the reference rules. `Abstractions`, `Dcb`, `ModuleHosting`, `Projections`, `Automations` and `TestKit` live in `Base/`.

- `Api` → each module → `Domain` → `Dcb`. Every module also references `EventStore`, `ModuleHosting` and `Projections`. `ModuleHosting` references `Dcb` and `EventStore`, and `Projections` references `Dcb`, `EventStore` and `ModuleHosting`. `Api` and every module also reference `Abstractions`. A module with automations also references `Automations`, which references `Abstractions`, `Dcb`, `EventStore` and `ModuleHosting`. `Dcb` and `Abstractions` reference nothing, and `Domain` references only `Dcb`. References only point this way
- A module never references another module, `Api` or ASP.NET Core; handlers return `CommandResult`, never `IResult`. New modules go into `Modules` in `Architecture.Tests`
- Modules talk only through the event log. Any module may read any event, in a state model, a projection or an automation, but each event type is appended by one module only, and a module never reads another module's tables. Each module declares exactly one `IModule` marker, and it is internal, so only the module itself can inject its `ModuleDatabase<TModule>`
- An automation appends nothing itself. It issues a command of its own module, whose handler appends, and depends neither on `EventStore` nor on the state models in `CCC.Domain.State`
- `Dcb`, `EventStore`, `ModuleHosting`, `Projections` and `Automations` know nothing about the domain. `EventStore` knows nothing about `ModuleHosting`, `Projections` or `Automations`, `ModuleHosting` knows nothing about `Projections` or `Automations`, and those two know nothing about each other. `Dcb` uses no package, not even Dapper or Npgsql
- Command handlers and automations live in a module's `Commands` namespace, query handlers and projections in its `Queries` namespace, and each endpoint on the same side as the handler it calls
- Command and query handlers are internal. Endpoints inject `ICommandHandler<TCommand>` or `IQueryHandler<TQuery, TResult>`, never a handler class
- Endpoints never touch the state models in `CCC.Domain.State`, `Tag`, `Dcb`, `ModuleHosting`, `Projections`, `Automations` or the event store directly. Only `Program.cs` references `EventStore` and `ModuleHosting`, to register them
- Value objects check format and throw `InvalidValueException`, which the API turns into a 400. Handlers check rules that need the event log
- Every append in `PostgresEventStore` holds one advisory lock until commit, so positions become visible strictly in order and projection bookmarks never skip an event. Don't append outside that lock
- Event records keep primitive types, and must never be renamed: the stored event type is the record's class name
