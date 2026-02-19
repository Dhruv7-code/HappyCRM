import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

function App() {
  return (
    <div className="min-h-screen bg-gray-100 flex flex-col">
      {/* Top navigation bar */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <span className="text-xl font-semibold text-gray-800">Internal Dashboard</span>
          <span className="text-sm text-gray-400">v0.1.0</span>
        </div>
      </header>

      {/* Main content area */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="bg-white rounded-2xl shadow p-8 text-center text-gray-500">
          <h1 className="text-2xl font-bold text-gray-700 mb-2">Welcome to the Internal Dashboard</h1>
          <p className="text-sm">Setup is complete. Start building features here.</p>
        </div>
      </main>

      {/* Footer */}
      <footer className="py-4 text-center text-xs text-gray-400">
        Internal use only &mdash; {new Date().getFullYear()}
      </footer>
    </div>
  )
}

export default App
