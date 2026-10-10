import { useEffect, useState } from 'react'
import { query } from './api'

export function useQuery(path) {
  const [version, setVersion] = useState(0)
  const [result, setResult] = useState({})

  useEffect(() => {
    if (!path) return
    let current = true
    query(path).then(
      data => { if (current) setResult({ path, version, data }) },
      error => { if (current) setResult({ path, version, error: error.message }) },
    )
    return () => { current = false }
  }, [path, version])

  const fresh = result.path === path
  return {
    data: fresh ? result.data : undefined,
    error: fresh ? result.error : undefined,
    loading: Boolean(path) && !(fresh && result.version === version),
    reload: () => setVersion(v => v + 1),
  }
}
