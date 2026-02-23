import { useState, useEffect } from 'react'
import AllCustomersTable from '../components/AllCustomersTable'
import NewCustomerForm from '../components/NewCustomerForm'
import { getAllCustomers } from '../api/customersApi'

export default function Customers() {
  const [customers, setCustomers] = useState([])
  const [panelOpen, setPanelOpen] = useState(false)

  useEffect(() => {
    getAllCustomers()
      .then(data => setCustomers(data))
      .catch(() => setCustomers([]))
  }, [])

  // Called by NewCustomerForm after a successful POST
  function handleCustomerAdded(newCustomer) {
    // Prepend to the table immediately — no re-fetch needed
    setCustomers(prev => [{ ...newCustomer, activeJobs: 0 }, ...prev])
  }

  return (
    <div className="flex flex-col h-full bg-gray-300 relative">

      {/* Page title */}
      <div className="px-8 py-7.5 border-b-2 border-white bg-gray-300 flex flex-row items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Customers</h1>
        <button
          onClick={() => setPanelOpen(true)}
          className="bg-gray-800 text-white text-xs font-semibold px-4 py-2 rounded-[5px] hover:bg-gray-700 transition-colors"
        >
          + Add Customer
        </button>
      </div>

      {/* Divider */}
      <div className="border-t border-white" />

      {/* Content */}
      <div className="px-8 py-6">
        <AllCustomersTable rows={customers} />
      </div>

      {/* ── Slide-in panel backdrop ── */}
      {panelOpen && (
        <div
          className="fixed inset-0 bg-black/30 z-40"
          onClick={() => setPanelOpen(false)}
        />
      )}

      {/* ── Slide-in panel ── */}
      <div
        className={`fixed top-0 right-0 h-full w-80 bg-white shadow-2xl z-50 flex flex-col transition-transform duration-300 ease-in-out ${
          panelOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {/* Panel header */}
        <div className="flex items-center justify-between px-6 py-5 border-b border-gray-200">
          <h2 className="text-sm font-bold text-gray-800">New Customer</h2>
          <button
            onClick={() => setPanelOpen(false)}
            className="text-gray-400 hover:text-gray-700 text-lg leading-none transition-colors"
          >
            ✕
          </button>
        </div>

        {/* Panel body */}
        <div className="flex-1 overflow-y-auto px-6 py-5">
          <NewCustomerForm onSuccess={handleCustomerAdded} />
        </div>
      </div>

    </div>
  )
}

