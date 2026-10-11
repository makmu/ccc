# ADR-009: Modules by Area of Work

## Decision

The backend's use cases are grouped into modules. A module covers one area of work: a bounded set of tasks that belong together. It holds the command records and handlers for those tasks, the projections and query handlers they need, and an `Add<Module>()` extension that registers them.

| Module | Area of work | Use cases |
|--------|--------------|-----------|
| `AppAdministration` | running the CMS: subscription models, tenant onboarding, users | `AddSubscriptionModel`, `RegisterTenant`, `AssignTenantOwner`, `AddUser` |
| `TenantAdministration` | managing a tenant and its teams | `AddTeam`, `RenameTenant` |
| `TeamAdministration` | setting up a team's projects and their languages | `AddProject`, `AddLanguage` |
| `TranslationEditor` | maintaining keys and their texts | `AddKey`, `RenameKey`, `ProvideReferenceText`, `ProvideTranslation` |
| `TranslationDelivery` | delivering translations to the client applications that consume them | `TranslationsOfProject` |

A use case goes into the module whose tasks it changes together with and whose queries it needs. Who usually performs a task is a hint, not the boundary: one role may work in several modules, and roles may change without any code moving. `AddLanguage` changes a project's setup, so it belongs to `TeamAdministration`, even though its effect shows up in `TranslationEditor`.

Every module is a project in `backend/Modules/<Module>`, in the solution folder `Modules`, with the root namespace `CCC.Modules.<Module>`. It keeps one file per use case: `Modules/TranslationEditor/AddKey.cs` holds the `AddKey` record and `AddKeyHandler`. Its endpoints live in `Api/<Module>/`. A module with projections owns one PostgreSQL schema named after it (`translation_editor`).

The modules sit side by side and never reference each other:

```
Api ──► AppAdministration ────┐
  ├───► TenantAdministration ─┤
  ├───► TeamAdministration ───┼──► Domain ──► EventStore
  ├───► TranslationEditor ────┤
  └───► TranslationDelivery ──┘
        (Api and every module also reference Abstractions)
```

- **Modules talk only through the event log.** Any module may read any event, in a state model or in a projection. Each event type is appended by exactly one module. A module never reads another module's tables.
- **`Domain` is the shared kernel.** Events, the tag vocabulary, state models and value objects stay in `Domain`, because the decisions of one module depend on facts written by others. `AddKey` in `TranslationEditor` checks `Subscription.KeyLimit`, which comes from `AppAdministration`, and `Team.Subscription`, which comes from `TenantAdministration`. The conditional append is only correct if every module builds the same tags for the same facts.
- **Authorization is not tied to modules.** Roles are granted permissions per use case. A role may use several modules, and a module's use cases may need different permissions.
- **"Slice" and "module".** A slice is one use case, such as `AddKey`. A module groups the slices of one area of work. ADR-007's "projection slices" are now modules.
- **Concurrency conflicts.** `StateContext.TryAppendAsync` in `EventStore` returns `false` when the conditional append fails, and handlers return `CommandResult.ConcurrentModification` from `Abstractions`. This replaces the `StateContextExtensions` in `Commands`, which no module could share.
- **Tests.** Every module has a `Modules/<Module>.Tests` project. A `TestKit` project holds `InMemoryEventStore`, the command fixture and its assertions. The fixture registers every module, so tests set up their state through other modules' commands and get the same events and tags as production. Test projects may reference several modules; production modules may not.

`Architecture.Tests` checks that no module references another module, `Api` or ASP.NET Core.

This decision replaces the `Commands` project from ADR-006 and the solution folder for projections from ADR-007. `PublicTranslations` becomes `TranslationDelivery`, and its schema becomes `translation_delivery`. ADR-007's projection mechanics, such as runners, bookmarks, rebuilds and the append lock, are unchanged.

## Context

Commands were grouped by technical role in `Commands`, and projections by consumer in their own solution folder. A single feature, such as archiving a key, touched a handler in `Commands`, a projection in another project and endpoints in two `Api` folders. The two halves of the backend followed different organizing rules.

Tasks that belong together change together. Editing keys and texts needs a view of a project's keys in every language, and changes to that view follow changes to the editing commands. Setting up projects needs a different view of the same projects.

The modules are not separate bounded contexts. They share one vocabulary and one event log, and a single decision such as `AddKey` reads facts that three modules wrote.

## Consequences

**Positive:**
- A feature lives in one module: its commands, handlers, projections and queries change together.
- Commands and queries follow the same organizing rule.
- Changing who may perform a task changes permissions, not the code structure.
- New modules are added without touching existing ones.

**Negative:**
- More projects: five modules, their test projects and `TestKit`.
- Module tests depend on the commands of other modules for their setup, so a bug in `AddProject` also fails `TranslationEditor` tests.
- Test projects of modules with projections need PostgreSQL.
- `Domain` stays shared and grows with every module.
- Which module a use case belongs to is sometimes a judgment call.
- The `public_translations` schema is left behind when its module is renamed and has to be dropped by hand. `translation_delivery` is rebuilt from the first event.

## Alternatives Considered

1. **Keep the split by technical role**
   - *Advantages:* No migration. Every handler is in one place.
   - *Disadvantages:* Features stay spread across projects, and commands and queries follow different organizing rules.

2. **One module per actor**
   - *Advantages:* Each module would map to one authorization policy.
   - *Disadvantages:* Roles overlap and change. A small tenant's owner may also set up its projects. The code structure would follow the organization chart, and every change in who does what would raise the question of moving code.

3. **Modules as bounded contexts with their own events**
   - *Advantages:* Modules would be fully independent and could be extracted into services.
   - *Disadvantages:* The modules share one vocabulary and one consistency model. `AddKey` would need facts translated across module boundaries, and tags would no longer be guaranteed to match.

4. **Modules per domain concept (`Keys`, `Projects`, `Teams`)**
   - *Advantages:* It matches the current folders in `Commands`.
   - *Disadvantages:* Tasks cut across concepts. Editing texts needs projects, languages and keys, while setting up a project needs a different view of the same projects.

5. **Folders per module in one project**
   - *Advantages:* Fewer projects.
   - *Disadvantages:* Nothing stops one module from using another's internals.

6. **Seeding raw events in module tests**
   - *Advantages:* Module tests would depend only on their own module.
   - *Disadvantages:* Tags are built by the handlers. Seeded events would need a second copy of that logic, and a mismatch would make tests pass or fail for the wrong reasons.

## Related Decisions

- ADR-002: Event Sourcing with Command-Context-Consistency
- ADR-006: Split the Backend into API, Commands, Domain and EventStore Projects
- ADR-007: Projections Answer Queries, One Project per Slice
- ADR-008: An Abstractions Project for the Handler Contracts
