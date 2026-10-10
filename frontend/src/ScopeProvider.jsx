import { useState } from 'react'
import { levels, ScopeContext } from './scope'

const storageKey = 'ccc.scope'

function restore() {
  try {
    return JSON.parse(localStorage.getItem(storageKey)) ?? {}
  } catch {
    return {}
  }
}

export default function ScopeProvider({ children }) {
  const [scope, setScope] = useState(restore)

  function choose(level, id) {
    const next = { ...scope, [level]: id }
    for (const lower of levels.slice(levels.indexOf(level) + 1)) delete next[lower]
    setScope(next)
    try {
      localStorage.setItem(storageKey, JSON.stringify(next))
    } catch {
      // The choice still holds until the page is reloaded.
    }
  }

  return <ScopeContext.Provider value={{ scope, choose }}>{children}</ScopeContext.Provider>
}
