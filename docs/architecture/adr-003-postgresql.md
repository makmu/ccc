# ADR-003: PostgreSQL as Event Log Database

## Decision

We will use **PostgreSQL** as the database system for the **event log**. Read model projections are not bound by this decision — each projection may choose the storage technology best suited to its query requirements (e.g. MongoDB, Redis, or any other).

## Context

The event sourcing architecture defined in ADR-002 requires a database for the event log that can:

- Reliably append events as immutable records with strong consistency guarantees.
- Support **conditional inserts** (e.g. optimistic concurrency checks) needed for Command-Context-Consistency — ensuring a command's context has not changed between read and write.
- Query and index **JSON/JSONB payloads** efficiently, as event data will be stored as structured JSON.
- Scale to large volumes of append-only data over time.

PostgreSQL satisfies all of these requirements while being well-known to the team, reducing operational and development friction.

Read models are rebuilt from the event log and are inherently replaceable, so they are free to use whichever store fits their access patterns best.

## Consequences

**Positive:**
- Native `JSONB` support with rich operators and GIN indexing enables efficient querying of event payloads.
- Strong ACID guarantees and support for conditional inserts (e.g. `INSERT ... WHERE`, row-level locking) make it well-suited for CCC-style consistency enforcement.
- Widely adopted, well-documented, and free and open-source with no licensing cost.
- The team is already familiar with PostgreSQL, minimising the learning curve.
- Decoupling the event log from projection storage allows each read model to optimise independently.

**Negative:**
- Requires hosting and operating a PostgreSQL instance (managed cloud options mitigate this).
- As the event log grows unboundedly, partitioning and archiving strategies will need attention over time.
- Allowing multiple storage technologies for projections introduces polyglot persistence complexity when projections grow in number.

## Alternatives Considered

1. **MS SQL Server** (for the event log)
   - *Advantages:* Familiar to .NET teams, strong tooling integration with the Microsoft ecosystem.
   - *Disadvantages:* Commercial licensing cost. JSON support exists but is less mature and less expressive than PostgreSQL's `JSONB`. Not free to run.

2. **MongoDB** (for the event log)
   - *Advantages:* Native document model maps naturally to JSON event payloads.
   - *Disadvantages:* Less familiar to the team. It is not confirmed that MongoDB's conditional write semantics cover all consistency scenarios required by CCC (see ADR-002).

## Related Decisions

- ADR-002: Event Sourcing with Command-Context-Consistency
