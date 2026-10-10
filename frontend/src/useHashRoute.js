import { useSyncExternalStore } from 'react'

const subscribe = onChange => {
  window.addEventListener('hashchange', onChange)
  return () => window.removeEventListener('hashchange', onChange)
}

const currentRoute = () => window.location.hash.replace(/^#\/?/, '')

export function useHashRoute() {
  return useSyncExternalStore(subscribe, currentRoute)
}
