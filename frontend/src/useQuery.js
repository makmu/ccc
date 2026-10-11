import { useEffect, useRef, useState } from 'react'
import { query } from './api'

const attempts = 15
const pause = 200

const load = path => query(path).then(
  data => ({ path, data }),
  error => ({ path, error: error.message }),
)

export function useQuery(path) {
  const [result, setResult] = useState({})
  const currentPath = useRef(path)

  useEffect(() => {
    currentPath.current = path
    if (!path) return
    let current = true
    load(path).then(loaded => { if (current) setResult(loaded) })
    return () => { current = false }
  }, [path])

  // Projections catch up shortly after a command, so a single reload could still miss the change.
  async function reloadUntil(caughtUp = () => true) {
    for (let attempt = 1; ; attempt++) {
      const loaded = await load(path)
      if (currentPath.current !== path) return
      setResult(loaded)
      if (loaded.error || caughtUp(loaded.data) || attempt === attempts) return
      await new Promise(resolve => setTimeout(resolve, pause))
    }
  }

  const fresh = result.path === path
  return {
    data: fresh ? result.data : undefined,
    error: fresh ? result.error : undefined,
    loading: Boolean(path) && !fresh,
    reloadUntil,
  }
}
