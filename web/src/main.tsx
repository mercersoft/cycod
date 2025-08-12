import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import Admin from './pages/Admin.tsx'
import GettingStarted from './pages/GettingStarted.tsx'
import TryItLive from './pages/TryItLive.tsx'
import ErrorPage from './pages/ErrorPage.tsx'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'

const router = createBrowserRouter([
  { path: '/', element: <App /> },
  { path: '/start', element: <GettingStarted /> },
  { path: '/live', element: <TryItLive /> },
  { path: '/admin', element: <Admin /> },
  { path: '*', element: <ErrorPage /> },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
