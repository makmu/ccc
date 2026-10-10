import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import ScopeProvider from './ScopeProvider.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ScopeProvider>
      <App />
    </ScopeProvider>
  </StrictMode>,
)
