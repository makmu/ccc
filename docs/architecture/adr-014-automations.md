# ADR-014: Automations React to Events by Issuing Commands

## Decision

An automation reacts to events by issuing a command. It never appends events itself. The command's handler runs the business checks, builds the tags and appends conditionally, as it does for an endpoint, so every event type is still appended by one module only.

The first automation is `DefaultTeamAutomation` in `TenantAdministration`. It reacts to `TenantRegistered` by issuing `AddDefaultTeam`, which adds a team named "Default" with the tenant's first subscription model.

- **Where it lives.** An automation belongs to the module whose command it issues, in that module's `Commands` namespace, because it only exists to issue commands, just as projections live in `Queries`. It sits in its own file next to the command, and the module registers it with `AddAutomation<T>()`.
- **The infrastructure.** A new project `Base/Automations` holds `IAutomation`, the `Reaction` delegate, `AutomationRunner<T>`, `AutomationHealthCheck<T>` and the `AddAutomation<T>` registration. An automation names a schema and registers one `Reaction` per event type, which issues the command and returns its `CommandResult`. The project references `Abstractions` for `CommandResult`, and `Projections` for `ProjectionDatabase`, `ProjectionRetryPolicy`, the `.With` helper and `EventLogRunner`.
- **One base class for both runners.** `EventLogRunner` in `Projections` follows the event log from a bookmark: it installs the bookmark, reads batches of the runner's event types, retries and stalls (ADR-010), and waits for the next append. `ProjectionRunner<T>` and `AutomationRunner<T>` derive from it and differ only in how they install their bookmark and process a batch. A projection applies a batch in one transaction with its bookmark. An automation reacts to one event at a time and moves its bookmark after each reaction.
- **The bookmark.** Each automation owns a schema in the projection database that holds only its bookmark: `position`, `stalled_at` and `stall_reason`. An automation has no `Version` and is never rebuilt, because a replay would issue its commands again. On its first start it begins at the start of the event log, so tenants that were registered before it existed and have no team also get a default team.
- **At least once.** The bookmark moves after each reaction, but no transaction covers both the command's append and the bookmark. After a crash between the two, the runner reacts to the same event again. A command issued by an automation therefore treats a repeated reaction as already done. `AddDefaultTeam` succeeds without appending when the tenant has any team (`Tenant.HasTeams`). That state model is part of the conditional append, so a second reaction, or a team that a user adds at the same time, can't lead to a second team.
- **Failures.** A command that returns an error counts as a failure, just like an exception. The retries and the stall from ADR-010 apply unchanged, and the stall reason names the event and the error, for example `Reacting to event 1 (TenantRegistered) failed: NotFound: Tenant not found.`. An outcome that should not hold back later events, such as "already done", is a success and not an error.
- **Health.** `/health` lists each automation under its schema name. It is healthy when caught up, degraded while events are left to react to, and unhealthy while not installed or stalled.
- **Tests.** The command is tested like any other, against the in-memory store. The automation is tested with `AutomationFixture` from `TestKit`, which runs it against PostgreSQL and sets up state through commands, since commands depend on tags.

`Architecture.Tests` checks that `Automations` knows nothing about the domain, that `Abstractions`, `Dcb`, `EventStore`, `Projections`, `Domain` and the endpoints don't depend on it, and that automations live in a `Commands` namespace.

## Context

So far, everything that changed the event log was a direct reaction to a request. Some rules state what should follow from a fact instead: a newly registered tenant should have a team to work in, without anyone adding it by hand.

The follow-up can't simply be appended together with the first event. `TenantRegistered` belongs to `AppAdministration` and `TeamAdded` to `TenantAdministration` (ADR-009), and registering a tenant shouldn't fail because adding its team does.

## Consequences

**Positive:**
- Every event is still appended by a command handler, with its checks, its tags and its conditional append.
- A repeated reaction, whether after a crash, a restart or a rewound bookmark, is harmless. The rule that makes it harmless is a state model, not infrastructure.
- Automations stall and report their health the same way projections do.

**Negative:**
- The default team appears shortly after the tenant is registered, not with it. A client that lists the teams right after registering may not see it yet.
- Every command that an automation issues has to treat a repeated reaction as done. Nothing checks this except its tests.
- Automations use `ProjectionDatabase`, `ProjectionRetryPolicy` and `EventLogRunner` from `Projections`, so that project is no longer only about projections, and two of these names say projection.
- `AutomationHealthCheck<T>` repeats most of `ProjectionHealthCheck<T>`.
- Like projections, an automation must run in one API instance only. Two instances would react to every event twice.
- `AddDefaultTeam` is a public command, so an endpoint could issue it, although no endpoint does.

## Alternatives Considered

1. **Append both events in `RegisterTenant`**
   - *Advantages:* One atomic decision, and no background work.
   - *Disadvantages:* `AppAdministration` would append `TeamAdded`, which belongs to `TenantAdministration`, and the tags of `TeamAdded` would be built in two modules.

2. **Let the automation append events itself**
   - *Advantages:* No command needed.
   - *Disadvantages:* It bypasses the business checks, the tags and the conditional append of the module that owns the event type.

3. **A todo list instead of a bookmark**
   - *Advantages:* The automation would keep no state. It would ask for "tenants without a team" on every append and issue a command for each, so crashes and rebuilds would heal themselves.
   - *Disadvantages:* Listing things needs a projection, or loading all tenants and teams on every append. The automation would also have to wait for that projection to catch up, which takes a signal from the projection runner that doesn't exist.

4. **Commit the bookmark together with the command's append**
   - *Advantages:* Each event would be reacted to exactly once.
   - *Disadvantages:* The bookmark would have to live in the event store database, and `IEventStore` would have to take part in a transaction opened outside it. The event store would have to know about automations.

5. **Put the automation infrastructure into `Projections`**
   - *Advantages:* One project fewer.
   - *Disadvantages:* `Projections` would hold automations, and `CCC.Base.Projections` would no longer say what the project is for. Sharing the runner logic doesn't need this, since `EventLogRunner` is enough.

6. **A team id derived from the tenant id**
   - *Advantages:* A repeated reaction would fail on the taken id, without a separate rule.
   - *Disadvantages:* It needs a name-based UUID in the domain, and a conflict would have to be read as success. "A tenant without a team gets a default team" is the actual rule and covers repeated reactions on its own.

7. **Two independent runners**
   - *Advantages:* Each runner reads on its own, without a base class.
   - *Disadvantages:* The retry, stall and wake-up logic would exist twice and could drift apart, although ADR-010 defines it once for both.

## Related Decisions

- ADR-002: Event Sourcing with Command-Context-Consistency
- ADR-009: Modules by Area of Work
- ADR-010: Projections Stall After Repeated Failures
- ADR-011: Projection Infrastructure in a ProjectionBase Project
- ADR-013: A Base Folder for the Shared Projects
