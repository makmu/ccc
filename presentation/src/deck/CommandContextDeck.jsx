import { useEffect, useRef } from 'react'
import { Code, Deck, Fragment, Slide, useReveal } from '@revealjs/react'
import RevealHighlight from 'reveal.js/plugin/highlight/highlight.esm.js'
import 'reveal.js/dist/reveal.css'
import 'reveal.js/dist/theme/black.css'
import 'reveal.js/plugin/highlight/monokai.css'
import './theme.css'
import {
  addKeyHandlerSnippet,
  eventStoreSnippet,
  projectionSnippet,
  stateLoaderSnippet,
  stateModelSnippet,
  tenantRenameHandlerSnippet,
  tenantRenameInsertSqlSnippet,
} from './codeSnippets.js'
import slicesDiagram from './slices.svg'

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

function SyncedCode({ children, lineNumbers, fragmentIndexes, language = 'csharp' }) {
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
      <Code language={language} lineNumbers={lineNumbers}>
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
              <strong>TL;DR</strong>
              <p>
              A practical event-sourcing model where every command rebuilds only the facts it needs,
              selected by event type and tags, decides locally, and appends new facts atomically.
              CCC over aggregates.</p>
            </div>
            <div className="signal-panel warm">
              <span></span>
              <strong>Also known as...</strong>
              <p>
              "Dynamic Consistency Boundary (DCB)"
              </p>
              <p>
              or "Kill the aggregate" 😉
              </p>
            </div>
            <div className="signal-panel">
              <span>Example</span>
              <strong>Calinga with CCC</strong>
              <p>Multi-tenant, key-value CMS for translation management.</p>
            </div>
          </div>
        </div>
        <aside className="notes">
        </aside>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Every system can be split into commands and queries</SectionLabel>
        <h2 className="slide-title">Command Query Separation </h2>
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
            <div className="command-flow-chip">Rename tenant Acme -&gt; Acme One</div>
          </div>
          <div className="command-flow-arrow">&rarr;</div>
          <div className="command-flow-step">
            <div className="command-flow-title">Event</div>
            <div className="command-flow-chip">TenantRenamed</div>
          </div>
          <div className="command-flow-arrow">&rarr;</div>
          <div className="command-flow-step command-flow-step-latest">
            <div className="command-flow-title">State</div>
            <div className="command-flow-chip">Current tenant name: Acme One</div>
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
            <h3>Projections are separate</h3>
            <p>Queries usually use projections optimized for reading, while the event log remains the source of truth.</p>
          </div>
        </div>
        <div className="event-stream-diagram" aria-label="Event stream with newest event appended on the right">
          <div className="event-stream-title">event stream</div>
          <div className="event-stream-track">
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenantRegistered</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenantOwnerAssigned</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">TenantRenamed</span>
            </div>
            <span className="event-arrow">&rarr;</span>
            <div className="event-node event-node-latest">
              <span className="event-dot"></span>
              <span className="event-label">TenantRenamed</span>
              <span className="event-badge">new</span>
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Why Event Sourcing?</SectionLabel>
        <h2 className="slide-title">Gains</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>Agile architecture</h3>
            <p>We do not need one giant unified model that must be constantly migrated and pre-optimized for every future use case.</p>
          </div>
          <div className="info-panel">
            <h3>Independent projections</h3>
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
        <SectionLabel>We must prevent inconsistent state</SectionLabel>
        <h2 className="slide-title">Consistency</h2>
        <div className="comparison-grid dense">
          <div className="comparison-panel muted">
            <h3>The race</h3>
            <p>Two admins of two different tenants try to rename their tenant to the same new value at almost the same time.</p>
          </div>
          <div className="comparison-panel accent">
            <h3>The risk</h3>
            <p>If both requests decide on stale information, the system can violate the business rule that a tenant name must be unique across the whole system.</p>
          </div>
        </div>
        <div className="event-stream-diagram" aria-label="Two concurrent rename operations from different tenants competing for the same target name">
          <div className="event-stream-title">Same intent, same time</div>
          <div className="event-stream-track">
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">Rename tenant Acme -&gt; Horizon</span>
            </div>
            <span className="event-arrow">+</span>
            <div className="event-node">
              <span className="event-dot"></span>
              <span className="event-label">Rename tenant Globex -&gt; Horizon</span>
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
        <SectionLabel>Aggregates as consistency boundaries</SectionLabel>
        <h2 className="slide-title">Aggregates</h2>
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
        <SectionLabel>Practical Example from existing Projects</SectionLabel>
        <h2 className="slide-title">Stream-enforced consistency</h2>
        <div className="split-layout">
          <div className="split-aside">
            <div className="info-panel">
              <h3>One event table</h3>
            <p>All events live in a single table and carry a `streamId` and a `streamVersion`.</p>
          </div>
          <div className="info-panel">
            <h3>Combined primary key</h3>
            <p>`(streamId, streamVersion)` is a primary key.</p>
          </div>
          <div className="info-panel">
            <h3>Race resolved by the database</h3>
            <p>Concurrent writes fail if they try to insert the same `streamId + version`.</p>
          </div>
          </div>
          <div className="split-main">
            <div className="event-table-mockup" aria-label="Example event table with stream id, stream version, type and payload columns">
          <div className="event-stream-title">event log with streams</div>
          <table className="event-table">
            <colgroup>
              <col style={{ width: '20%' }} />
              <col style={{ width: '20%' }} />
              <col style={{ width: '26%' }} />
              <col style={{ width: '34%' }} />
            </colgroup>
            <thead>
              <tr>
                <th>Stream Id</th>
                <th>Stream Version</th>
                <th>Type</th>
                <th>Payload</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td><code>tenant-42</code></td>
                <td><code>1</code></td>
                <td><code>TenantRegistered</code></td>
                <td><code>{'{ name: "Acme" }'}</code></td>
              </tr>
              <tr>
                <td><code>tenant-99</code></td>
                <td><code>3</code></td>
                <td><code>TenantOwnerAssigned</code></td>
                <td><code>{'{ userId: "u-17" }'}</code></td>
              </tr>
              <tr>
                <td><code>...</code></td>
                <td><code>...</code></td>
                <td><code>...</code></td>
                <td><code>...</code></td>
              </tr>
              <tr>
                <td><code>tenant-42</code></td>
                <td><code>8</code></td>
                <td><code>TenantRenamed</code></td>
                <td><code>{'{ name: "Acme One" }'}</code></td>
              </tr>
            </tbody>
          </table>
          <div className="event-table-insert-attempt" aria-label="Conflicting row insert that fails because stream id and stream version already exist">
            <div className="event-table-attempt-header">
              <div className="event-table-note event-table-note-centered">-- Second user tries to rename the tenant --</div>
            </div>
            <table className="event-table event-table-conflict">
              <colgroup>
                <col style={{ width: '20%' }} />
                <col style={{ width: '20%' }} />
                <col style={{ width: '26%' }} />
                <col style={{ width: '34%' }} />
              </colgroup>
              <thead>
                <tr>
                  <th>Stream Id</th>
                  <th>Stream Version</th>
                  <th>Type</th>
                  <th>Payload</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td><code>tenant-42</code></td>
                  <td><code>8</code></td>
                  <td><code>TenantRenamed</code></td>
                  <td><code>{'{ name: "Acme Prime" }'}</code></td>
                </tr>
              </tbody>
            </table>
            <p className="event-table-error"><span className="event-table-fail-badge">X</span> INSERT fails: duplicate key for `(streamId = tenant-42, streamVersion = 8)`</p>
            </div>
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>The 2 big problems with this approach</SectionLabel>
        <h2 className="slide-title">Main Drawbacks</h2>
        <div className="comparison-grid dense">
          <div className="comparison-panel muted">
            <h3>We lose architectural agility</h3>
            <p>We are back to designing aggregates up front: </p>
            <p>which ones exist, what their boundaries are, and which rules must live inside them.</p>
            <p>Refactoring or re-scoping them later becomes a real pain.</p>
          </div>
          <div className="comparison-panel accent">
            <h3>Some rules cross aggregate boundaries</h3>
            <p>For example: </p>
            <p>how do we guarantee that a tenant name stays unique across the whole system when tenants are registered or renamed in different streams?</p>
          </div>
        </div>
        <p className="slide-kicker">
          The mechanism works well inside one aggregate stream, but system-wide rules do not always fit neatly into those boundaries.
        </p>
      </Slide>

      <Slide className="ccc-slide" autoAnimate>
        <SectionLabel>The Core Idea</SectionLabel>
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
            <p>Load the smallest relevant slice of history required to make validation and consistency decisions <b>for this command</b>: the right event types, carrying the right tags.</p>
          </div>
          <div className="process-step">
            <div className="process-step-heading">
              <span>3</span>
              <h3>Consistency</h3>
            </div>
            <p>Append new events, with their tags, only if that slice of history has not changed since it was read.</p>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Events carry tags</SectionLabel>
        <h2 className="slide-title">Tags</h2>
        <div className="three-column-grid">
          <div className="info-panel">
            <h3>Written on append</h3>
            <p>Every command tags the events it appends with the things they are about, such as <code>tenant-id=42</code> or <code>tenant-name=horizon</code>.</p>
          </div>
          <div className="info-panel">
            <h3>Selected by type and tags</h3>
            <p>A context is a list of (event type, tags) pairs. An event matches when its type matches and it carries all of those tags.</p>
          </div>
          <div className="info-panel">
            <h3>Indexed by PostgreSQL</h3>
            <p>Tags are a <code>TEXT[]</code> column with a GIN index, so a containment query like <code>{"tags @> '{tenant-name=horizon}'"}</code> stays fast.</p>
          </div>
        </div>
        <div className="event-table-mockup" aria-label="One event with its tags and a context filter that matches it by type and tag">
          <div className="event-stream-title">one event, three tags</div>
          <table className="event-table event-table-tagged">
            <colgroup>
              <col style={{ width: '10%' }} />
              <col style={{ width: '20%' }} />
              <col style={{ width: '38%' }} />
              <col style={{ width: '32%' }} />
            </colgroup>
            <thead>
              <tr>
                <th>Position</th>
                <th>Type</th>
                <th>Tags</th>
                <th>Payload</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td><code>43</code></td>
                <td><code>TenantRenamed</code></td>
                <td><code>tenant-id=42</code> <code>tenant-name=acme</code> <code>tenant-name=horizon</code></td>
                <td><code>{'{ TenantId: 42, Name: "Horizon" }'}</code></td>
              </tr>
            </tbody>
          </table>
          <p className="context-filter-chip">
            <span className="context-filter-label">context</span>
            <span>{"type = 'TenantRenamed' AND tags @> '{tenant-name=horizon}'"}</span>
          </p>
        </div>
        <p className="slide-kicker">
          The rename is tagged with the old and the new name, so later name checks see it whether it took a name or freed one.
        </p>
      </Slide>

      <Slide className="ccc-slide">
        <SectionLabel>Concurrency without streams</SectionLabel>
        <h2 className="slide-title">dynamic consistency boundary</h2>
        <div className="split-layout">
          <div className="split-aside">
            <div className="comparison-panel muted">
              <h3>No streamId, no version</h3>
              <p>Every event lands in one global log with a single growing <code>position</code>. There are no stream boundaries to design up front.</p>
            </div>
            <div className="comparison-panel accent">
              <h3>The command picks its own boundary</h3>
              <p>A rename only cares about its own tenant and the name it wants: events tagged <code>tenant-id</code> of that tenant or <code>tenant-name=horizon</code>.</p>
            </div>
            <div className="comparison-panel muted">
              <h3>Unrelated commands don't race</h3>
              <p>Renaming another tenant to Umbrella at the same time succeeds, because its context shares no tag with these two.</p>
            </div>
          </div>
          <div className="split-main">
            <div className="event-table-mockup" aria-label="One global event log where two concurrent renames to the same name overlap in the tenant-name tag, so only the first append succeeds">
              <div className="event-stream-title">event log with tags</div>
              <table className="event-table event-table-tagged">
                <colgroup>
                  <col style={{ width: '10%' }} />
                  <col style={{ width: '24%' }} />
                  <col style={{ width: '40%' }} />
                  <col style={{ width: '26%' }} />
                </colgroup>
                <thead>
                  <tr>
                    <th>Pos</th>
                    <th>Type</th>
                    <th>Tags</th>
                    <th>Payload</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td><code>40</code></td>
                    <td><code>TenantRegistered</code></td>
                    <td><code>tenant-id=42</code> <code>tenant-name=acme</code></td>
                    <td><code>{'{ Name: "Acme" }'}</code></td>
                  </tr>
                  <tr>
                    <td><code>41</code></td>
                    <td><code>TenantRegistered</code></td>
                    <td><code>tenant-id=99</code> <code>tenant-name=globex</code></td>
                    <td><code>{'{ Name: "Globex" }'}</code></td>
                  </tr>
                  <tr>
                    <td><code>42</code></td>
                    <td><code>KeyAdded</code></td>
                    <td><code>team-id=3</code> <code>key-id=12</code> <code>...</code></td>
                    <td><code>{'{ Name: "welcome" }'}</code></td>
                  </tr>
                </tbody>
              </table>
              <div className="context-filter-stack">
                <p className="context-filter-chip">
                  <span className="context-filter-label">Acme</span>
                  <span>tenant-id=42 OR tenant-name=horizon &rarr; MAX(position) = <strong>40</strong></span>
                </p>
                <p className="context-filter-chip">
                  <span className="context-filter-label">Globex</span>
                  <span>tenant-id=99 OR tenant-name=horizon &rarr; MAX(position) = <strong>41</strong></span>
                </p>
              </div>
              <div className="event-table-insert-attempt" aria-label="Two concurrent inserts, one succeeds and one fails because the shared tag now has a newer event">
                <table className="event-table event-table-tagged event-table-success">
                  <colgroup>
                    <col style={{ width: '10%' }} />
                    <col style={{ width: '24%' }} />
                    <col style={{ width: '40%' }} />
                    <col style={{ width: '26%' }} />
                  </colgroup>
                  <tbody>
                    <tr>
                      <td><code>&rarr; 43</code></td>
                      <td><code>TenantRenamed</code></td>
                      <td><code>tenant-id=42</code> <code>tenant-name=acme</code> <code>tenant-name=horizon</code></td>
                      <td><code>{'{ Name: "Horizon" }'}</code></td>
                    </tr>
                  </tbody>
                </table>
                <p className="event-table-error event-table-ok"><span className="event-table-fail-badge event-table-win-badge">OK</span> Acme appends first: its context still ends at 40, so the rename gets position 43.</p>
                <table className="event-table event-table-tagged event-table-conflict">
                  <colgroup>
                    <col style={{ width: '10%' }} />
                    <col style={{ width: '24%' }} />
                    <col style={{ width: '40%' }} />
                    <col style={{ width: '26%' }} />
                  </colgroup>
                  <tbody>
                    <tr>
                      <td><code>44?</code></td>
                      <td><code>TenantRenamed</code></td>
                      <td><code>tenant-id=99</code> <code>tenant-name=globex</code> <code>tenant-name=horizon</code></td>
                      <td><code>{'{ Name: "Horizon" }'}</code></td>
                    </tr>
                  </tbody>
                </table>
                <p className="event-table-error"><span className="event-table-fail-badge">X</span><span>Globex fails: 43 carries <code>tenant-name=horizon</code>, so its context now ends at 43 &ne; 41.</span></p>
              </div>
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>One question, one state model</SectionLabel>
            <h2 className="slide-title">State Models</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>A question about the domain, declared once in <code>Domain</code></Fragment>
              <Fragment as="li" index={1}>Initial state</Fragment>
              <Fragment as="li" index={2}>Tags select the events</Fragment>
              <Fragment as="li" index={3}>One reducer per event type</Fragment>
              <Fragment as="li" index={4}>Is the name taken? Asked across all tenants, by the name tag alone</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|3-4|5|6|7-10|12-18" fragmentIndexes={[0, 1, 2, 3, 4]}>
              {stateModelSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Renaming a tenant</SectionLabel>
            <h2 className="slide-title">Code Example</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>The endpoint turns the request into a <code>RenameTenant</code> command</Fragment>
              <Fragment as="li" index={1}>Load three state models in one call</Fragment>
              <Fragment as="li" index={2}>Decision 1: the tenant exists</Fragment>
              <Fragment as="li" index={3}>Same name again: nothing to do</Fragment>
              <Fragment as="li" index={4}>Decision 2: the new name is available</Fragment>
              <Fragment as="li" index={5}>Append <code>TenantRenamed</code> with its tags, only if nothing changed concurrently</Fragment>
              <Fragment as="li" index={6}>The result becomes 200, 404 or 409</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-5|7-12|14-15|17-18|20-22|24-31|33-35" fragmentIndexes={[0, 1, 2, 3, 4, 5, 6]}>
              {tenantRenameHandlerSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Loading the context</SectionLabel>
            <h2 className="slide-title">State Loader</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Takes every state model of the command</Fragment>
              <Fragment as="li" index={1}>The context filter is the union of their filters</Fragment>
              <Fragment as="li" index={2}>One query, so all models see the same snapshot</Fragment>
              <Fragment as="li" index={3}>Each event goes to the models whose filter it matches</Fragment>
              <Fragment as="li" index={4}>Remember the last position read</Fragment>
              <Fragment as="li" index={5}>Filter and position become the append condition</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-2|4-6|9-10|12-16|18-19|22" fragmentIndexes={[0, 1, 2, 3, 4, 5]}>
              {stateLoaderSnippet}
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
              <Fragment as="li" index={0}>Position and filter from the context, tags for the new event</Fragment>
              <Fragment as="li" index={1}>Serialize event data to JSON</Fragment>
              <Fragment as="li" index={2}>SQL where clause from the context filter</Fragment>
              <Fragment as="li" index={3}>Additional SQL parameters</Fragment>
              <Fragment as="li" index={4}>Build the conditional SQL insert</Fragment>
              <Fragment as="li" index={5}>Execute it under the append lock and fail if no row was inserted</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-4|6|7-8|10-15|17-25|27-33" fragmentIndexes={[0, 1, 2, 3, 4, 5]}>
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
              <Fragment as="li" index={0}>One advisory lock for every append: positions become visible strictly in order</Fragment>
              <Fragment as="li" index={1}>Insert the renamed event with its tags</Fragment>
              <Fragment as="li" index={2}>If no event in the context came after position 40</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode language="sql" lineNumbers="|1-2|4-9|10-21" fragmentIndexes={[0, 1, 2]}>
              {tenantRenameInsertSqlSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Adding a new key</SectionLabel>
            <h2 className="slide-title">More Complex Example</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Five state models across team, project and key</Fragment>
              <Fragment as="li" index={1}>Existence and uniqueness checks</Fragment>
              <Fragment as="li" index={2}>Second load for the key limit, which never changes, so it stays out of the append condition</Fragment>
              <Fragment as="li" index={3}>The team's key count is in the condition: concurrent adds can't exceed the limit</Fragment>
              <Fragment as="li" index={4}>Tag the event with what future commands will ask about</Fragment>
            </ol>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|1-9|11-22|24-25|27-30|32-40" fragmentIndexes={[0, 1, 2, 3, 4]}>
              {addKeyHandlerSnippet}
            </SyncedCode>
          </div>
        </div>
      </Slide>
      <Slide className="ccc-slide">
        <SectionLabel>Why I chose SQL over NoSQL</SectionLabel>
        <h2 className="slide-title">SQL vs. NoSQL</h2>
        <div className="three-column-grid">
          <div className="info-panel accent-border">
            <h3>PostgreSQL</h3>
            <p>JSONB payloads, <code>TEXT[]</code> tags with a GIN index, ACID transactions and conditional inserts map directly to the consistency checks this approach needs.</p>
          </div>
          <div className="info-panel accent-border">
            <h3>Why not MongoDB?</h3>
            <p>I wanted strong transactional behavior, explicit conditional inserts and an advisory lock that orders every append. With SQL, those guarantees are straightforward and easy to reason about.</p>
          </div>
          <div className="info-panel accent-border">
            <h3>Result</h3>
            <p>The consistency mechanism stays visible in understandable SQL instead of depending on hidden framework behavior or unclear database semantics.</p>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid slices-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>One more thing...</SectionLabel>
            <h2 className="slide-title">Vertical Slices</h2>
            <ul className="editorial-list tight">
              <li><b>One module per area of work</b></li>
              <li><b>Modules talk only through the event log</b></li>
              <li><b>Each event type is appended by one module</b></li>
              <li><b>Each module owns its projections</b></li>
              <li><b>Grow without refactoring</b></li>
            </ul>
          </div>
          <div className="example-code">
            <div className="slices-diagram">
              <img src={slicesDiagram} alt="Three modules — translation editor, translation delivery and tenant administration — each with its own projection schema, independent but reading from and writing to one shared append-only event stream" />
            </div>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-code ccc-slide-code-example">
        <div className="slide-example-grid">
          <div className="code-explainer example-copy">
            <SectionLabel>Projections</SectionLabel>
            <h2 className="slide-title">Query Side</h2>
            <ol className="editorial-list tight">
              <Fragment as="li" index={0}>Each module owns one PostgreSQL schema</Fragment>
              <Fragment as="li" index={1}>Raise the version and the schema is rebuilt from the first event</Fragment>
              <Fragment as="li" index={2}>One handler per event type, which also decides which events the runner reads</Fragment>
              <Fragment as="li" index={3}>Each event becomes exactly one SQL command</Fragment>
            </ol>
            <p>A background runner applies new events right after every append and reports its health at <code>/health</code>.</p>
          </div>
          <div className="example-code">
            <SyncedCode lineNumbers="|3|5|7-13|17-23" fragmentIndexes={[0, 1, 2, 3]}>
              {projectionSnippet}
            </SyncedCode>
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
              <li>consistency boundaries as small as the rule</li>
              <li>commands only conflict when their contexts share events</li>
              <li>small state models, reused across commands and queries</li>
            </ul>
          </div>
          <div className="tradeoff-card caution">
            <h3>Costs</h3>
            <ul className="editorial-list tight">
              <li>developers must think in events and tags, not aggregates</li>
              <li>tags are written on append: a new rule may need a tag that old events lack</li>
              <li>every append waits for one global lock</li>
              <li>less debuggable than streams, because there are no stream boundaries to read along</li>
            </ul>
          </div>
        </div>
      </Slide>

      <Slide className="ccc-slide ccc-slide-close" backgroundGradient="linear-gradient(135deg, #130b1d 0%, #0d1733 45%, #050816 100%)">
        <SectionLabel>Summary</SectionLabel>
        <h2 className="slide-title">Takeaways</h2>
        <div className="closing-summary-grid">
          <ul className="editorial-list">
            <li>Event sourcing gives us architectural agility.</li>
            <li>Aggregates protect consistency, but they also lock in boundaries.</li>
            <li>CCC keeps consistency and moves the boundary to the command.</li>
            <li>Tags and state models make that boundary as small as the rule.</li>
          </ul>
          <div className="closing-band">
            <div className="closing-band-qr">
              <svg viewBox="0 0 98 96" width="160" height="160" role="img" aria-label="GitHub">
                <path
                  fill="#fff8eb"
                  d="M48.854 0C21.839 0 0 21.743 0 48.64c0 21.514 13.98 39.77 33.362 46.21 2.427.453 3.316-1.043 3.316-2.317 0-1.148-.04-4.19-.064-8.225-13.572 2.93-16.437-6.482-16.437-6.482-2.218-5.594-5.417-7.082-5.417-7.082-4.43-3 .335-2.94.335-2.94 4.9.34 7.48 5 7.48 5 4.357 7.399 11.432 5.26 14.22 4.023.444-3.131 1.705-5.26 3.103-6.47-10.834-1.22-22.227-5.37-22.227-23.911 0-5.278 1.9-9.594 5.017-12.977-.508-1.22-2.175-6.143.472-12.8 0 0 4.09-1.297 13.398 4.957a46.94 46.94 0 0 1 12.2-1.617c4.138.019 8.307.557 12.2 1.617 9.307-6.254 13.39-4.957 13.39-4.957 2.655 6.657.988 11.58.48 12.8 3.124 3.383 5.01 7.7 5.01 12.977 0 18.587-11.412 22.678-22.279 23.878 1.754 1.505 3.316 4.482 3.316 9.036 0 6.522-.056 11.78-.056 13.385 0 1.284.88 2.79 3.34 2.312C84.03 88.402 98 70.148 98 48.64 98 21.743 76.153 0 48.854 0Z"
                />
              </svg>
            </div>
            <div className="closing-band-copy">
              <p><a href="https://github.com/makmu/ccc">{'</>'} https://github.com/makmu/ccc</a></p>
            </div>
          </div>
        </div>
        <p className="closing-thanks">Thank you! 🙏</p>
        <div className="closing-reading">
          <p>Further reading</p>
          <ul className="closing-reading-list">
            <li><a href="https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-am-here-to-kill-the-aggregate.html">Kill the Aggregate - https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-am-here-to-kill-the-aggregate.html</a></li>
            <li><a href="https://dcb.events/">dcb.events - https://dcb.events/</a></li>
            <li><a href="https://ralfwestphal.substack.com/s/event-orientation">Ralf Westphal: Event-Orientation - https://ralfwestphal.substack.com/s/event-orientation</a></li>
          </ul>
        </div>
      </Slide>
    </Deck>
  )
}
