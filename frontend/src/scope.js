import { createContext, useContext } from 'react'

export const levels = ['tenant', 'team', 'project']

export const ScopeContext = createContext(null)

export const useScope = () => useContext(ScopeContext)
