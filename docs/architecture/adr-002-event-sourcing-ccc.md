# ADR-002: Event Sourcing with Command-Context-Consistency

## Decision

We will use **Event Sourcing** as the primary backend architecture for state management and persistence. The core design principle is **Command-Context-Consistency (CCC)**: each command operates on a minimal, locally-rebuilt context derived from the event log, and produces new events as its only output.

## Context

CCC is a lightweight approach to event sourcing that avoids the complexity of traditional DDD aggregates and shared object models. Instead of maintaining a central mutable state, each feature independently rebuilds only the facts it needs — directly from the event stream — to evaluate its business rules and decide what to append.

The model works as follows:

- **Command** — an intent to change the system (e.g. `CreateTenant`, `AddTranslation`). Commands are the entry point for all mutations.
- **Context** — a minimal, locally-scoped projection of past events, built on-demand for the specific decision a command must make. No shared aggregates, no global state.
- **Consistency** — enforced per command by reading the relevant slice of the event log and writing new events atomically. The event log is the single source of truth.

Business logic becomes a pure function: `(context, command) → events[]`. This eliminates side effects from decision-making and makes each feature independently testable and evolvable.

Read models (projections) are built per use-case from the event log and are never the authoritative state.

## Consequences

**Positive:**
- Full audit trail — every state change is a recorded fact, never overwritten.
- Business logic is pure and independently testable per command.
- Features are isolated; no coupling through a shared domain model.
- Projections can be added, changed, or rebuilt at any time without touching core logic.
- The architecture scales naturally as new commands and read models can be introduced without refactoring existing ones.

**Negative:**
- Querying current state requires maintaining projections; there is no single "current state" table to query directly.
- Developers unfamiliar with event sourcing face an initial learning curve.
- Care is needed to keep event schemas stable and backwards-compatible as the system evolves.

## Alternatives Considered

1. **CRUD with a relational model**
   - *Advantages:* Familiar, straightforward to query, easy to set up.
   - *Disadvantages:* Loses history, couples features through shared mutable state, harder to evolve business rules without risking data integrity.

2. **Traditional DDD with Aggregates**
   - *Advantages:* Well-documented pattern with strong consistency guarantees.
   - *Disadvantages:* Requires upfront modeling of aggregate boundaries, which are difficult to get right and expensive to change. Shared aggregate models introduce coupling across features.

## References

- Rico Fritzsche, *Simplicity Wins: Command-Context-Consistency* — https://ricofritzsche.me/simplicity-wins-command-context-consistency/
