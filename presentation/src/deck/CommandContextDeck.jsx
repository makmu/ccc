import { useEffect, useRef } from 'react'
import { Code, Deck, Fragment, Slide, Stack, useReveal } from '@revealjs/react'
import RevealHighlight from 'reveal.js/plugin/highlight/highlight.esm.js'
import 'reveal.js/dist/reveal.css'
import 'reveal.js/dist/theme/black.css'
import 'reveal.js/plugin/highlight/monokai.css'
import './theme.css'
import {
  eventStoreSnippet,
  keyEndpointSnippet,
  organizationRenameInsertSqlSnippet,
  organizationRenameEndpointSnippet,
} from './codeSnippets.js'

const deckConfig = {
  hash: true,
  controls: true,
  progress: true,
  center: false,
  width: 1440,
  height: 900,
  margin: 0.06,
  transition: 'fade',
  backgroundTransition: 'fade',
  slideNumber: 'c/t',
}

function SectionLabel({ children }) {
  return <p className="section-label">{children}</p>
}

function StatCard({ value, label }) {
  return (
    <div className="stat-card">
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  )
}

function QuotePanel({ children, footer }) {
  return (
    <figure className="quote-panel">
      <blockquote>{children}</blockquote>
      <figcaption>{footer}</figcaption>
    </figure>
  )
}

function SyncedCode({ children, lineNumbers, fragmentIndexes }) {
  const wrapperRef = useRef(null)
  const deck = useReveal()

  useEffect(() => {
    const wrapper = wrapperRef.current
    if (!wrapper) {
      return
    }

    const applyIndexes = () => {
      const fragments = wrapper.querySelectorAll('code.fragment')
      if (fragments.length === 0) {
        return false
      }

      fragments.forEach((fragment, index) => {
        fragment.setAttribute('data-fragment-index', String(fragmentIndexes[index] ?? index))
      })

      const section = wrapper.closest('section')
      if (section && deck && typeof deck.syncFragments === 'function') {
        deck.syncFragments(section)
      }

      return true
    }

    if (applyIndexes()) {
      return
    }

    const observer = new MutationObserver(() => {
      if (applyIndexes()) {
        observer.disconnect()
      }
    })

    observer.observe(wrapper, { childList: true, subtree: true })

    return () => observer.disconnect()
  }, [deck, fragmentIndexes])

  return (
    <div ref={wrapperRef}>
      <Code language="csharp" lineNumbers={lineNumbers}>
        {children}
      </Code>
    </div>
  )
}

export default function CommandContextDeck() {
  return (
    <Deck config={deckConfig} plugins={[RevealHighlight]}>
      <Slide className="ccc-slide ccc-slide-cover" backgroundGradient="linear-gradient(135deg, #050816 0%, #0d1733 58%, #29141e 100%)">
        <div className="cover-grid">
          <div>
            <SectionLabel>cp Open Space 2026</SectionLabel>
            <h1 className="cover-title">Command Context Consistency</h1>
            <p className="cover-subtitle">
              Markus Müller
            </p>
          </div>
          <div className="cover-aside">
            <div className="signal-panel warm">
              <span></span>
              <strong>TLDR;</strong>
              <p>
              A practical event-sourcing model where every command rebuilds only the facts it needs,
              decides locally, and appends new facts atomically. No aggregate needed.</p>
            </div>
            <div className="signal-panel warm">
              <span></span>
              <strong>Also known as...</strong>
              <p>
              "Slay the aggregate" or Dynamic Consistency Boundary (DCB).
              </p>
            </div>
            <div className="signal-panel">
              <span>Example</span>
              <strong>Calinga with CCC</strong>
              <p>Multi-tenent, key-value CMS for translations management.</p>
            </div>
          </div>
        </div>
        <aside className="notes">
        </aside>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Every system can be split into commands and queries</SectionLabel>
        <h2 className="slide-title">Command Query Segregation</h2>
        <div className="comparison-grid dense">
          <div className="comparison-panel muted">
            <h3>Commands</h3>
            <p>Commands change the system. They should return nothing but success or error, and they should be expressed in domain language rather than CRUD terminology.</p>
          </div>
          <div className="comparison-panel accent">
            <h3>Queries</h3>
            <p>Queries return information about the current state. They should never change the system.</p>
          </div>
        </div>
        <div className="command-flow-diagram" aria-label="Flow from command to event to resulting state, contrasted with queries reading state">
          <div className="command-flow-step">
            <div className="command-flow-title">Command</div>
            <div className="command-flow-chip">Rename tenent Acme -&gt; Acme One</div>
          </div>
          <div className="command-flow-arrow">&rarr;</div>
          <div className="command-flow-step">
            <div className="command-flow-title">Event</div>
            <div className="command-flow-chip">TenentRenamed</div>
          </div>
          <div className="command-flow-arrow">&rarr;</div>
          <div className="command-flow-step command-flow-step-latest">
            <div className="command-flow-title">State</div>
            <div className="command-flow-chip">Current tenent name: Acme One</div>
          </div>
        </div>
        <p className="slide-kicker">
          In this talk, we focus on the command side: how to change the system safely.
        </p>
      </Slide>

      <Slide className="ccc-slide" backgroundGradient="linear-gradient(180deg, rgba(5, 8, 22, 0.92), rgba(13, 23, 51, 0.88))">
        <SectionLabel>the short version of event sourcing</SectionLabel>
        <h2 className="slide-title">Event Sourcing Recap</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>Store facts as events</h3>
            <p>Instead of overwriting rows, we append immutable facts such as `UserAdded` or `KeyRenamed`.</p>
          </div>
          <div className="info-panel">
            <h3>Rebuild state from history</h3>
            <p>The current picture is derived by replaying relevant events, not by trusting a single mutable object.</p>
          </div>
          <div className="info-panel">
            <h3>Read models are separate</h3>
            <p>Queries usually use projections optimized for reading, while the event log remains the source of truth.</p>
          </div>
        </div>
        <div className="event-stream-diagram" aria-label="Event stream with newest event appended on the right">
          <div className="event-stream-title">event stream</div>
          <div className="event-stream-track">
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenentRegistered</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenentOwnerAssigned</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenentRenamed</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node event-node-latest">
              <span className="event-dot"></span>
              <span className="event-label">TenentRenamed</span>
              <span className="event-badge">new</span>
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Why Event Sourcing?</SectionLabel>
        <h2 className="slide-title">Why we use event sourcing</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>Agile architecture</h3>
            <p>We do not need one giant unified model that must be constantly migrated and pre-optimized for every future use case.</p>
          </div>
          <div className="info-panel">
            <h3>Independent read models</h3>
            <p>Each new usage can get its own projection instead of forcing more compromises into a single shared schema.</p>
          </div>
          <div className="info-panel">
            <h3>Full history</h3>
            <p>Every change is stored as a fact, so we keep the source material needed to rebuild, explain, or reshape the system later.</p>
          </div>
        </div>
        <p className="slide-kicker">
          Event sourcing lets the write side stay stable while the read side evolves with the product.
        </p>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Consistency</SectionLabel>
        <h2 className="slide-title">We must prevent inconsistent state</h2>
        <div className="comparison-grid dense">
          <div className="comparison-panel muted">
            <h3>The race</h3>
            <p>Two admins of two different tenents try to rename their tenent to the same new value at almost the same time.</p>
          </div>
          <div className="comparison-panel accent">
            <h3>The risk</h3>
            <p>If both requests decide on stale information, the system can violate the business rule that a tenent name must be unique across the whole system.</p>
          </div>
        </div>
        <div className="event-stream-diagram" aria-label="Two concurrent rename operations from different tenents competing for the same target name">
          <div className="event-stream-title">Same intent, same time</div>
          <div className="event-stream-track">
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">Rename tenent Acme -&gt; Horizon</span>
            </div>
            <span className="event-arrow">+</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">Rename tenent Globex -&gt; Horizon</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node event-node-latest">
              <span className="event-dot"></span>
              <span className="event-label">Name must stay unique</span>
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Classic Approach</SectionLabel>
        <h2 className="slide-title">Aggregates as consistency boundaries</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>In DDD and classic event sourcing</h3>
            <p>Consistency is usually modeled around aggregates: focused parts of the domain with their own rules.</p>
          </div>
          <div className="info-panel">
            <h3>Often backed by substreams</h3>
            <p>Each aggregate gets its own stream or substream, and the system ensures that this local stream stays consistent.</p>
          </div>
          <div className="info-panel">
            <h3>Why not one global boundary?</h3>
            <p>Treating the whole system as one fully consistent stream is not practical, because unrelated changes would constantly race with each other.</p>
          </div>
        </div>
        <p className="slide-kicker">
          Aggregates solve the problem by saying: these things must be consistent together, and everything else can evolve independently.
        </p>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Practical Example from Zollner Projects</SectionLabel>
        <h2 className="slide-title">Stream-enforced consistency</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>One event table</h3>
            <p>All events live in a single table, but each event carries a `streamId` and a `streamVersion`.</p>
          </div>
          <div className="info-panel">
            <h3>Combined primary key</h3>
            <p>The table uses `(streamId, streamVersion)` as a primary key, so every next event must claim the next version number in that stream.</p>
          </div>
          <div className="info-panel">
            <h3>Race resolved by the database</h3>
            <p>If two concurrent writes try to insert the same `streamId + version`, one succeeds and one fails.</p>
          </div>
        </div>
        <div className="event-table-mockup" aria-label="Example event table with stream id, stream version and payload columns">
          <div className="event-stream-title">event table</div>
          <table className="event-table">
            <thead>
              <tr>
                <th>Stream Id</th>
                <th>Stream Version</th>
                <th>Payload</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td><code>tenent-42</code></td>
                <td><code>1</code></td>
                <td><code>{'{ type: "TenentRegistered", name: "Acme" }'}</code></td>
              </tr>
              <tr>
                <td><code>tenent-99</code></td>
                <td><code>3</code></td>
                <td><code>{'{ type: "TenentOwnerAssigned", userId: "u-17" }'}</code></td>
              </tr>
              <tr>
                <td><code>...</code></td>
                <td><code>...</code></td>
                <td><code>...</code></td>
              </tr>
              <tr>
                <td><code>tenent-42</code></td>
                <td><code>8</code></td>
                <td><code>{'{ type: "TenentRenamed", name: "Acme One" }'}</code></td>
              </tr>
            </tbody>
          </table>
          <div className="event-table-insert-attempt" aria-label="Conflicting row insert that fails because stream id and stream version already exist">
            <div className="event-table-attempt-header">
              <div className="event-table-note event-table-note-centered">-- Second user tries to rename the tenent --</div>
            </div>
            <table className="event-table event-table-conflict">
              <thead>
                <tr>
                  <th>Stream Id</th>
                  <th>Stream Version</th>
                  <th>Payload</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td><code>tenent-42</code></td>
                  <td><code>8</code></td>
                  <td><code>{'{ type: "TenentRenamed", name: "Acme Prime" }'}</code></td>
                </tr>
              </tbody>
            </table>
            <p className="event-table-error"><span className="event-table-fail-badge">X</span> INSERT fails: duplicate key for `(streamId = tenent-42, streamVersion = 8)`</p>
          </div>
        </div>
        <p className="slide-kicker">
          The application proposes the next version. The database enforces that only one writer can actually claim it.
        </p>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Main Drawbacks</SectionLabel>
        <h2 className="slide-title">The two big problems with this approach</h2>
        <div className="comparison-grid dense">
          <div className="comparison-panel muted">
            <h3>We lose architectural agility</h3>
            <p>We are back to designing aggregates up front: which ones exist, what their boundaries are, and which rules must live inside them. Refactoring or re-scoping them later becomes a real pain.</p>
          </div>
          <div className="comparison-panel accent">
            <h3>Some rules cross aggregate boundaries</h3>
            <p>For example: how do we guarantee that a tenent name stays unique across the whole system when tenents are registered or renamed in different streams?</p>
          </div>
        </div>
        <p className="slide-kicker">
          The mechanism works well inside one aggregate stream, but system-wide rules do not always fit neatly into those boundaries.
        </p>
      </Slide>

      <Slide className="ccc-slide" autoAnimate>
        <SectionLabel>Core Idea</SectionLabel>
        <h2 className="slide-title">Command Context Consistency</h2>
        <div className="process-strip">
          <div className="process-step">
            <div className="process-step-heading">
              <span>1</span>
              <h3>Command</h3>
            </div>
            <p>An intent enters the system. Example: rename tenant.</p>
          </div>
          <div className="process-step">
            <div className="process-step-heading">
              <span>2</span>
              <h3>Context</h3>
            </div>
            <p>Load the smallest relevant slice of history required to make validation and consistency decisions.</p>
          </div>
          <div className="process-step">
            <div className="process-step-heading">
              <span>3</span>
              <h3>Consistency</h3>
            </div>
            <p>Append new events only if that slice of history has not changed since it was read.</p>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Renaming a tenent</SectionLabel>
            <h2 className="slide-title">Code Example</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Request</Fragment>
              <Fragment as="li" index={1}>Context</Fragment>
              <Fragment as="li" index={2}>State slice replay</Fragment>
              <Fragment as="li" index={3}>Decision 1: valid tenant id</Fragment>
              <Fragment as="li" index={4}>Decision 2: new tenant name available</Fragment>
              <Fragment as="li" index={5}>Append `OrganizationRenamed` only if nothing changed concurrently.</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-4|6-9|11-26|28-29|31-36|38-48" fragmentIndexes={[0, 1, 2, 3, 4, 5]}>
              {organizationRenameEndpointSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Appending to the Event Store</SectionLabel>
            <h2 className="slide-title">Digging Deeper</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Raw method signature to append events to the store</Fragment>
              <Fragment as="li" index={1}>Serialize event data to JSON</Fragment>
              <Fragment as="li" index={2}>SQL where clauses from the context filter</Fragment>
              <Fragment as="li" index={3}>Additional SQL parameters</Fragment>
              <Fragment as="li" index={4}>Build the conditional SQL insert</Fragment>
              <Fragment as="li" index={5}>Execute the statement and fail if event could not be inserted</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-6|8-8|9-10|12-17|19-27|29-38" fragmentIndexes={[0, 1, 2, 3, 4, 5]}>
              {eventStoreSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>PostgreSQL Statement</SectionLabel>
            <h2 className="slide-title">Final SQL</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Insert the concrete renamed event</Fragment>
              <Fragment as="li" index={1}>If the relevant event slice has not change</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-5|6-11" fragmentIndexes={[0, 1]}>
              {organizationRenameInsertSqlSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Adding a new key</SectionLabel>
            <h2 className="slide-title">Another Example</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Endpoint path and input parameters</Fragment>
              <Fragment as="li" index={1}>Build the context from the relevant event types</Fragment>
              <Fragment as="li" index={2}>Rebuild state slice</Fragment>
              <Fragment as="li" index={3}>Check the subscription model limit</Fragment>
              <Fragment as="li" index={4}>Check whether the key name is still available</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-7|9-16|18-19|21-24|26-32" fragmentIndexes={[0, 1, 2, 3, 4]}>
              {keyEndpointSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>In Practice</SectionLabel>
        <h2 className="slide-title">Why I chose SQL over NoSQL here</h2>
        <div className="three-column-grid">
          <div className="info-panel accent-border">
            <h3>PostgreSQL</h3>
            <p>JSONB queries, ACID guarantees, and conditional writes map directly to the consistency checks this approach needs.</p>
          </div>
          <div className="info-panel accent-border">
            <h3>Why not MongoDB?</h3>
            <p>I wanted strong transactional behavior and explicit conditional inserts. With SQL, those guarantees are straightforward and easy to reason about.</p>
          </div>
          <div className="info-panel accent-border">
            <h3>Result</h3>
            <p>The consistency mechanism stays visible in understandable SQL instead of depending on hidden framework behavior or unclear database semantics.</p>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Command Context Consistency in practice</SectionLabel>
        <h2 className="slide-title">Tradeoffs</h2>
        <div className="tradeoff-grid">
          <div className="tradeoff-card good">
            <h3>Strengths</h3>
            <ul className="editorial-list tight">
              <li>more agile architecture</li>
              <li>supports dynamic consistency boundaries</li>
              <li>much more independent command logic</li>
            </ul>
          </div>
          <div className="tradeoff-card caution">
            <h3>Costs</h3>
            <ul className="editorial-list tight">
              <li>developers must think in events only, not aggregates</li>
              <li>context filters can become complicated</li>
              <li>performance must be watched carefully</li>
              <li>less debuggable than classic stream-based event sourcing because there are no clear stream boundaries</li>
            </ul>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-close" backgroundGradient="linear-gradient(135deg, #130b1d 0%, #0d1733 45%, #050816 100%)">
        <SectionLabel>Summary</SectionLabel>
        <h2 className="slide-title">Takeaways</h2>
        <ul className="editorial-list">
          <li>Commands and queries stay separate.</li>
          <li>Events preserve domain facts.</li>
          <li>Aggregates reduce agility.</li>
          <li>CCC uses dynamic consistency boundaries.</li>
          <li>Conditional writes ensure consistency.</li>
        </ul>
        <div className="closing-band">
          <p>Thank you.</p>
          <p><a href="https://github.com/your-org/ccc">github.com/your-org/ccc</a></p>
        </div>
      </Slide>
    </Deck>
  )
}
