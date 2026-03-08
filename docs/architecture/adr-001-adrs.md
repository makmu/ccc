# ADR-001: Use Architecture Decision Records

## Decision

We will use **Architecture Decision Records (ADRs)** to document significant architectural decisions for the COLI project. ADRs are stored as markdown files in the `docs/architecture/` directory of the repository, versioned alongside the code.

### ADR Format

Every ADR must follow this structure:

```
# ADR-NNN: Title

## Decision
What was decided and how it will be implemented.

## Context
The problem or situation that motivated the decision.

## Consequences
The resulting effects of the decision — both positive and negative.

## Alternatives Considered
Other options that were evaluated and why they were not chosen.
```

Additional sections (e.g., References, Related Decisions, Rationale) may be appended after Alternatives Considered when relevant.

### File Naming

Files are named `adr-NNN-short-name.md`, where `NNN` is a zero-padded sequential number and `short-name` is a kebab-case descriptor (e.g., `adr-001-adrs.md`).

## Context

As the COLI project grows, architectural decisions are made in conversations, meetings, or ad hoc discussions. Without a formal record, the reasoning behind these decisions is lost over time, making it difficult for current and future team members to understand why the system is built the way it is.

We need a lightweight, low-ceremony way to capture these decisions that fits naturally into our development workflow.

## Consequences

**Positive:**
- Decisions and their rationale are preserved and discoverable.
- ADRs are versioned with the code — the documentation always matches the state of the software at any given tag or branch.
- New team members can read the ADR history to understand the project's architectural evolution.
- The markdown format is easy to read, write, and review in pull requests.
- AI coding agents can parse ADRs directly from the repository and enforce or adhere to the rules and conventions laid out in them.

**Negative:**
- Requires discipline to write an ADR when a significant decision is made.
- ADRs can become outdated if not maintained alongside code changes.

## Alternatives Considered

1. **Wiki-based documentation**
   - *Advantages:* Can be shared via link without requiring repository access. Offers more what-you-see-is-what-you-get editing.
   - *Disadvantages:* Versioning is much more complicated — especially when different software versions or tags require different documentation, which is free with git. A wiki is a separate system not colocated with the code, so developers must look in multiple places. The wiki also needs to be secured independently of the git repository.
