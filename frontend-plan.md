# Frontend Plan

The frontend is a simple version of the CMS as its users would see it: the people who run the service, tenant owners, team members setting up projects, and editors writing texts. It hides all technical detail. No routes, IDs, status codes or JSON appear on screen.

Commands and queries still show, through the shape of the UI:

- **Every action is a command.** Buttons and dialogs are named after the command and do exactly one thing: "Add key", "Rename key", "Provide translation". There are no generic edit forms that save several changes at once.
- **Every list or overview is a query.** Screens show what a read model returns and refresh after a command.
- **Rejected commands explain themselves.** A business rule violation, such as a taken name or the key limit, appears next to the action in plain language.

Each step below is built, checked against the running backend, and committed on its own.

## Design

Plain and quiet, with nothing drawing attention away from the tasks.

| Token | Hex | Use |
|-------|-----|-----|
| `--paper` | `#FFFFFF` | background |
| `--ink` | `#1B1F24` | text |
| `--muted` | `#5B6470` | secondary text |
| `--rule` | `#D5DAE0` | dividers, input borders, table lines |
| `--action` | `#2F4FB5` | primary buttons, selected menu item |
| `--error` | `#B42318` | rejected commands, invalid input |

`system-ui` throughout, with no web fonts and no extra dependencies. Dark mode redefines the same tokens.

```
┌───────────────────────┬─────────────────────────────────────────────────┐
│ CCC                   │ Acme ▾ / Web team ▾ / Shop ▾                    │
│                       ├─────────────────────────────────────────────────┤
│ App administration    │ Translation editor                [Add key]     │
│ Tenant administration │                                                 │
│ Team administration   │ Key             English (ref)   German   French │
│ Translation editor  ◂ │ checkout.title  Checkout        Kasse    —      │
│ Translation delivery  │ checkout.pay    Pay now         —        —      │
│                       │                                                 │
└───────────────────────┴─────────────────────────────────────────────────┘
```

Left-aligned throughout. Actions open small dialogs or inline editors next to what they change. On narrow screens the menu collapses into a select.

## Steps

### 1. App shell

Frontend only, with no backend changes.

- A menu with the five areas of work, named for users: App administration, Tenant administration, Team administration, Translation editor and Translation delivery. Each area gets an empty page with its title and a short sentence saying what it is for.
- Simple routing via the URL hash, so an area can be bookmarked and the browser's back button works. This needs no router dependency.
- A context switcher in the header (tenant → team → project), remembered in the browser. Areas that work inside a tenant, team or project use it. Later steps fill its lists from queries.
- Shared building blocks:
  - an action dialog that runs a command and shows the reason when it is rejected
  - a list or table view that loads a query, with loading and empty states
- One API client. It turns 400, 404 and 409 responses into messages for the UI and generates new IDs.
- The Vite proxy forwards `/api/*` to the backend, so new routes need no proxy changes.
- `AddUserForm` and `RegisterTenantForm` are removed. Their functions come back in step 2. The Vite template leftovers are removed too.

### 2. App administration

For the people who run the service.

- **Backend**: an `app_administration` read model with the queries `SubscriptionModels`, `Tenants` and `Users`, plus their endpoints, `Api.http` entries and tests.
- **UI**: three sections on one page.
  - **Subscription models**: a list with its key limits and an "Add subscription model" action.
  - **Tenants**: a list with each tenant's owner. Actions: "Register tenant" (choosing subscription models) and "Assign owner" (choosing a user).
  - **Users**: a list and an "Add user" action.

### 3. Tenant administration

For a tenant's owner, working in the tenant chosen in the header.

- **Backend**: a `tenant_administration` read model with `TeamsOfTenant`. `Tenant` (its name and subscription models) is replayed from the tenant's events.
- **UI**:
  - the tenant's name with a "Rename tenant" action
  - a list of its teams with "Add team" (choosing one of the tenant's subscription models)

### 4. Team administration

For a team, working in the team chosen in the header.

- **Backend**: a `team_administration` read model with `ProjectsOfTeam`, including each project's languages and their parent languages.
- **UI**:
  - a list of projects with "Add project" (name and reference language)
  - per project, its languages as a fallback tree with "Add language" (choosing the parent language)

### 5. Translation editor

For editors, working in the project chosen in the header.

- **Backend**: a `translation_editor` read model with `KeysOfProject`: every key with its reference text and its translation per language.
- **UI**:
  - a key × language table and an "Add key" action
  - per key, "Rename key"
  - per cell, "Provide reference text" or "Provide translation" in a dialog that shows the reference text while translating
- Read models update shortly after a command. The table updates the changed row immediately and refreshes it from the query once the read model has caught up, so the delay never shows up as a missing change.

### 6. Translation delivery

For developers wiring the texts into their application, working in the project chosen in the header.

- **Backend**: a `LanguagesOfProject` query for the language picker, replayed from the project's events.
- **UI**: query only.
  - pick a language and see the translations a client application receives for it, with the fallback to parent languages applied
  - the address the application fetches them from, ready to copy

## Queries in the Backend

- **Lists get a read model.** Keys, translations, projects, teams, tenants, users and subscription models are listed from a projection in the module's own schema. Each module projects only what its screens need, even when that duplicates another module's data, because a module never reads another module's tables.
- **Details of a single entity are replayed.** A query for one tenant's name or one project's languages loads a state model through `StateLoader` and folds its events in memory. It needs no table.
- Step 2 adds this rule to `docs/project-structure.md`.

## Open Points

- There is no login. Areas are open to everyone, and the header context stands in for "the tenant, team or project I work in".
- JavaScript stays as it is. Switching to TypeScript would be a separate step.
