# Dependency Rules

What each project holds is described in [project-structure.md](project-structure.md). `Architecture.Tests` checks the reference rules.

- `Api` → each module → `Domain` → `EventStore`. `Api` and every module also reference `Abstractions`, which references nothing. References only point this way
- A module never references another module, `Api` or ASP.NET Core; handlers return `CommandResult`, never `IResult`. New modules go into `Modules` in `Architecture.Tests`
- Modules talk only through the event log. Any module may read any event, in a state model or a projection, but each event type is appended by one module only, and a module never reads another module's tables
- `EventStore` knows nothing about the domain
- Command and query handlers are internal. Endpoints inject `ICommandHandler<TCommand>` or `IQueryHandler<TQuery, TResult>`, never a handler class
- Endpoints never touch the state models in `CCC.Domain.State`, `Tag` or the event store directly. Only `Program.cs` references `EventStore`, to register it
- Value objects check format and throw `InvalidValueException`, which the API turns into a 400. Handlers check rules that need the event log
- Every append in `PostgresEventStore` holds one advisory lock until commit, so positions become visible strictly in order and projection bookmarks never skip an event. Don't append outside that lock
- Event records keep primitive types, and must never be renamed: the stored event type is the record's class name
