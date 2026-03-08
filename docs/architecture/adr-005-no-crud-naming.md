# ADR-005: No CRUD Terminology in Requests, Commands, and Events

## Decision

CRUD verbs (`Create`, `Read`, `Update`, `Delete`) are **forbidden** in the names of requests, commands, and events. All names must use intent-revealing, domain-specific language instead.

**Examples:**

| Forbidden | Preferred |
|-----------|-----------|
| `CreateUser` | `AddUser`, `RegisterUser` |
| `UpdateOrganization` | `RenameOrganization`, `ChangeOrganizationPlan` |
| `DeleteKey` | `RemoveKey`, `ArchiveKey` |
| `CreateTranslation` | `PublishTranslation`, `ProvideTranslation` |

This convention applies to all C# record and class names for requests, commands, and events, as well as to event type strings written to the event store.

## Context

CCC is built on Event Sourcing. In an event-sourced system, events and commands describe *what happened* or *what is intended*, not the database operation that results from it. CRUD names (`CreateUser`, `UpdateOrganization`) are implementation-level, storage-oriented terms — they describe the effect on a database row, not the business intent.

Using CRUD terminology leaks persistence concerns into the domain model, making the codebase harder to reason about and the event log less meaningful as a business record.

## Consequences

**Positive:**
- The event log reads as a meaningful history of business actions rather than a sequence of database mutations.
- Domain language is consistent across the codebase, event store, and conversations with stakeholders.
- Forces deliberate thought about what an operation actually means in the domain.

**Negative:**
- Requires discipline and occasional discussion to find the right domain verb.
- Reviewers must actively enforce the convention in code review.

## Alternatives Considered

1. **Allow CRUD names as a shorthand for simple operations**
   - *Disadvantage:* Erodes the domain model incrementally. Once a few CRUD names appear, the convention breaks down and the codebase becomes inconsistent.
