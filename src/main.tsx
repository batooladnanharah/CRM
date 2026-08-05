import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import PropsApp from './props/App.tsx'
import StateApp from './state/App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* Phase 1 & 2: Components + JSX */}
    <App />
    <hr />
    {/* Phase 3: Props */}
    <PropsApp />
    <hr />
    {/* Phase 4: State */}
    <StateApp />
  </StrictMode>,
)
