import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Sidebar from './pages/Sidebar'
import Dashboard from './pages/Dashboard'
import Customers from './pages/Customers'
import IntegrationJobs from './pages/IntegrationJobs'
import Execution from './pages/Execution'

function App() {
  return (
    <BrowserRouter>
      <div className="flex h-screen w-screen overflow-hidden">

        {/* ── Left panel — 20% width, dark grey ── */}
        <aside className="w-[20%] h-full bg-gray-800 shrink-0">
          <Routes>
            <Route path="/*" element={<Sidebar />} />
          </Routes>
        </aside>

        {/* ── Right panel — remaining 80%, white ── */}
        <main className="flex-1 h-full bg-white overflow-y-auto">
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/customers" element={<Customers />} />
            <Route path="/integration-jobs" element={<IntegrationJobs />} />
            <Route path="/execution" element={<Execution />} />
          </Routes>
        </main>

      </div>
    </BrowserRouter>
  )
}

export default App
