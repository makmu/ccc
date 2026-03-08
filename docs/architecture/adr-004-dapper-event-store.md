# ADR-004: Dapper as Event Log Data Access Library

## Decision

We will use **Dapper** (with Npgsql) as the data access library for reading and writing the event log in PostgreSQL (see ADR-003).

## Context

The event sourcing architecture (ADR-002) requires:

- Appending events as immutable JSON records to the event log.
- Reading events by position or filter to rebuild command contexts and projections.
- JSON serialization and deserialization of event payloads — not object-relational mapping.
- Precise control over SQL, including PostgreSQL-specific syntax (JSONB operators, conditional inserts for CCC optimistic concurrency, GIN index usage, CTEs, etc.).

Dapper is a minimal query helper over ADO.NET that binds parameters and maps result rows to C# objects, while leaving all SQL fully in the developer's hands. Npgsql provides the underlying PostgreSQL driver.

## Consequences

**Positive:**
- Full control over SQL — any PostgreSQL-specific syntax can be used directly without fighting framework abstractions.
- Optimistic concurrency checks for CCC can be expressed as precise conditional inserts in SQL.
- No framework conventions to work around; the event log is just a table and queries are just SQL.
- Minimal dependency surface — Dapper is a small, stable, well-understood library.
- Easy to reason about exactly what hits the database.

**Negative:**
- Schema creation and management must be handled manually (SQL scripts or startup helper).
- JSON dispatch (mapping the `type` string to the correct C# event class for deserialization) must be implemented by hand.
- Optimistic concurrency logic must be written explicitly — no built-in support.

## Alternatives Considered

1. **Marten**
   - *Advantages:* Purpose-built event store for PostgreSQL, automatic schema management, built-in optimistic concurrency, native JSONB handling.
   - *Disadvantages:* Marten is built around the concepts of **streams and aggregates** — events belong to a stream, and state is reconstituted by replaying a stream into an aggregate. This is fundamentally at odds with Command-Context-Consistency (ADR-002), where each command builds its own minimal context from an arbitrary slice of the event log, with no notion of a fixed aggregate boundary. Adopting Marten would mean working against its core model rather than with it.

2. **Entity Framework Core**
   - *Advantages:* Familiar to many .NET developers, handles schema migrations.
   - *Disadvantages:* Designed for object-relational mapping and entity change tracking — none of which apply to an append-only event log. Large framework for a simple three-column table.

## Related Decisions

- ADR-002: Event Sourcing with Command-Context-Consistency
- ADR-003: PostgreSQL as Event Log Database
