import { areas } from './areas'
import { useScope } from './scope'
import ScopeSwitcher from './ScopeSwitcher'
import { navigate, useHashRoute } from './useHashRoute'

export default function App() {
  const route = useHashRoute()
  const area = areas.find(a => a.route === route) ?? areas[0]

  return (
    <div className="shell">
      <nav className="menu" aria-label="Areas">
        <p className="brand">CCC</p>
        <ul>
          {areas.map(a => (
            <li key={a.route}>
              <a href={`#/${a.route}`} aria-current={a === area ? 'page' : undefined}>{a.title}</a>
            </li>
          ))}
        </ul>
        <select
          className="menu-compact"
          aria-label="Area"
          value={area.route}
          onChange={event => navigate(event.target.value)}
        >
          {areas.map(a => <option key={a.route} value={a.route}>{a.title}</option>)}
        </select>
      </nav>
      <main>
        {area.scope && <ScopeSwitcher depth={area.scope} />}
        <header className="area-header">
          <h1>{area.title}</h1>
          <p className="quiet">{area.purpose}</p>
        </header>
        <AreaContent area={area} />
      </main>
    </div>
  )
}

function AreaContent({ area }) {
  const { scope } = useScope()
  if (area.scope && !scope[area.scope]) {
    return <p className="hint">Choose a {area.scope} above to start.</p>
  }
  return area.Page ? <area.Page /> : null
}
