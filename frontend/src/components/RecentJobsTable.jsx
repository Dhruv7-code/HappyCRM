import { useState, useEffect } from 'react'
import { getRecentJobExecutions } from '../api/jobsApi'

/**
 * RecentJobsTable
 *
 * Displays the 5 most recent job executions.
 * Columns: Job Name | Customer | Status | Failure Type | Executed At
 * Styling: horizontal lines only, no vertical borders.
 *          Header: bg-gray-300 / text-gray-800
 *          Rows:   bg-white / text-black
 */

const COLUMNS = ['Job Name', 'Customer', 'Status', 'Failure Type', 'Executed At']

const statusColor = (status) => {
  if (status === 'Success') return 'text-green-600'
  if (status === 'Failed' || status === 'PermanentlyFailed') return 'text-red-500'
  return 'text-yellow-500'
}

const formatDate = (iso) => {
  const d = new Date(iso)
  return d.toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' })
}

export default function RecentJobsTable() {
  const [rows, setRows] = useState([])

  useEffect(() => {
    getRecentJobExecutions()
      .then(data => setRows(data))
      .catch(() => setRows([]))
  }, [])

  return (
    <div className="w-full overflow-hidden rounded-[5px] border border-gray-200">
      <table className="w-full border-collapse text-xs">
        <thead>
          <tr className="bg-gray-300">
            {COLUMNS.map((col) => (
              <th
                key={col}
                className="text-left text-gray-800 font-semibold px-4 py-3 border-b border-gray-300 border-x-0"
                style={{ borderLeft: 'none', borderRight: 'none' }}
              >
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr className="bg-white">
              <td colSpan={5} className="px-4 py-3 text-center text-gray-400">
                Loading…
              </td>
            </tr>
          ) : (
            rows.map((row, i) => (
              <tr key={i} className="bg-white border-b border-gray-200 last:border-b-0">
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.jobName}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.customerName}</td>
                <td className={`px-4 py-3 font-medium ${statusColor(row.status)}`} style={{ borderLeft: 'none', borderRight: 'none' }}>{row.status}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.failureType ?? '—'}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{formatDate(row.executedAt)}</td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
