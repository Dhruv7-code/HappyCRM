import { useState, useEffect } from 'react'
import { getAllExecutions } from '../api/jobsApi'
import ExecutionsTable from '../components/ExecutionsTable'

export default function Execution() {
  const [rows, setRows]       = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState(null)

  useEffect(() => {
    getAllExecutions()
      .then(setRows)
      .catch(err => setError(err.message ?? 'Failed to load executions'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="flex flex-col h-full bg-gray-300">

      {/* Page title */}
      <div className="px-8 py-8 border-b-2 border-white bg-gray-300 flex flex-row items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Executions</h1>
      </div>

      {/* Divider */}
      <div className="border-t border-white" />

      {/* Content */}
      <div className="px-8 py-6">
        {loading && (
          <p className="text-sm text-gray-500">Loading executions…</p>
        )}
        {error && (
          <p className="text-sm text-red-500">{error}</p>
        )}
        {!loading && !error && (
          <ExecutionsTable rows={rows} />
        )}
      </div>

    </div>
  )
}
