# ADR-007: Read Models as Projections, One Project per Slice

> **Changed by ADR-009:** read model slices are now modules under `Modules/`, which also hold the commands of their area of work. `PublicTranslations` is now `TranslationDelivery`, with the schema `translation_delivery`. The projection mechanics below are unchanged.

> **Changed by ADR-010:** a runner no longer retries failures forever. It stalls after three retries, records the stall in its bookmark table, and doesn't catch up until it is unstalled.

> **Changed by ADR-011:** the projection infrastructure has moved from `EventStore` into its own `ReadModels` project. `AppendSignal` stays in `EventStore`.

## Decision

Queries are answered from read models. Each read model is a vertical slice with its own project in the `ReadModels` solution folder, and each serves one kind of consumer. `PublicTranslations` serves client applications, and a richer slice for the translation management UI will sit next to it. A slice owns one PostgreSQL schema named after it, so a new slice never touches the tables of another. The slices sit next to the command side, not on top of it, and never reference each other:

```
Api ──► Commands ──────────► Domain ──► EventStore
  └───► PublicTranslations ──┘
```

Everything that more than one slice needs lives in `EventStore`: `IProjection`, `ProjectionRunner<T>`, `AppendSignal`, `ReadModelDatabase` and the `AddProjection<T>` and `AddReadModelDatabase` registrations. None of it knows about the domain. A slice contains only its projection, its query records and handlers, and an `Add<Slice>()` extension. The handler interfaces it shares with `Commands` live in `Abstractions` (ADR-008).

- **Projections.** A projection (`IProjection`) names its schema, a version and the event types it handles, and applies one `EventRecord` at a time with Dapper and plain SQL. `ProjectionRunner<T>` is a `BackgroundService` that reads new events with `IEventStore.ReadAsync`, applies them in batches, and in the same transaction moves the bookmark stored in the slice's own schema. A crash therefore never leaves a batch half applied.
- **Waking up.** After every committed append, `PostgresEventStore` signals each runner through a bounded channel of size one, so appends made while a runner is catching up are merged into a single extra pass. A runner catches up only on startup and after a signal, so events written outside this process, such as by a script, show up after the next restart.
- **One writer per read model.** Exactly one runner writes a schema, so the read model tables need no locking. Running the API more than once would break this.
- **Rebuilds.** Raising a projection's `Version` drops its schema on the next start and rebuilds it from the first event.
- **Precomputed answers.** A read model stores its answers ahead of time, not the raw facts. The translations slice keeps one row per key and project language. That row already holds the text the language inherits from its nearest ancestor with its own text, with the reference language at the root of the tree.
- **Queries.** A slice exposes query records and result records, which Dapper fills straight from the query. Its query handlers are internal and registered under `IQueryHandler<TQuery, TResult>` from `Abstractions` (ADR-008). The endpoints stay in `Api`, in a folder named after the slice, and slices do not depend on ASP.NET Core.
- **Positions become visible in order.** Every append in `PostgresEventStore` holds one transaction-scoped advisory lock from `INSERT` to `COMMIT`. Without it, two appends could commit out of position order, and a runner that has already moved its bookmark past the later position would never see the earlier one. Under the lock, `READ COMMITTED` is enough for the conditional append, so the `SERIALIZABLE` retry loop is gone.

## Context

The frontend needs the keys of a project with their texts in one language, including the texts a language inherits from its parent languages. Folding events on every request would repeat the inheritance walk for every read, and would grow with the event log.

Positions come from a `BIGSERIAL`, which hands out numbers before commit. `SERIALIZABLE` only promises that the result equals *some* serial order. The order "later position, then the reader, then the earlier position" is a valid serial order, so PostgreSQL never aborts it, and the reader silently skips an event.

## Consequences

**Positive:**
- New read models are added as new projects, without changing `Commands`, `Domain`, the event store or other slices.
- Each slice's schema can be dropped and rebuilt on its own.
- Queries are single indexed reads with no inheritance logic at read time.
- A bookmark never skips an event.

**Negative:**
- Read models are eventually consistent. A `GET` right after a `POST` can return the previous state for a few milliseconds.
- All appends go through one lock, which caps write throughput at one append at a time. This is fine for a CMS.
- Each slice's tests need a PostgreSQL server, because the projection logic lives in SQL.
- `EventStore` now also hosts the projection infrastructure and depends on `Microsoft.Extensions.Hosting.Abstractions`.
- A runner reads the log in pages of up to 100 events, one transaction per page, so a rebuild takes one round trip per page.

## Alternatives Considered

1. **Folding events on every query**
   - *Advantages:* No tables, no bookmark, always consistent.
   - *Disadvantages:* The cost grows with the number of events per project, and inheritance is resolved again on every read.

2. **PostgreSQL `LISTEN`/`NOTIFY` to wake the runners**
   - *Advantages:* It also sees appends from other processes.
   - *Disadvantages:* Either the event store has to send `NOTIFY`, or a trigger couples the read models to the event table. The in-process signal is enough for a single instance.

3. **A settle window, only applying events older than a few seconds**
   - *Advantages:* No change to the event store.
   - *Disadvantages:* It is a heuristic. A transaction that takes longer than the window is still skipped.

4. **Transaction IDs (`xid8` and `pg_snapshot_xmin`) as the bookmark**
   - *Advantages:* Appends stay concurrent.
   - *Disadvantages:* It needs a schema change and a two-part bookmark, which is much harder to reason about than one lock.
