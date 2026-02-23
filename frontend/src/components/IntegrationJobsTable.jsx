import { useState, useEffect } from 'react'
import { retryJob } from '../api/jobsApi'

/**
 * IntegrationJobsTable
 *
 * Props:
 *   rows  {Array<{ id, jobName, customerName, status, lastExecutedAt, retryCount }>}
 *
 * Columns: Job Name | Customer | Status | Last Execution | Retry Count | Actions
 * Footer:  "Rows per page: 5"  |  < [page] >
 */

const COLUMNS = ['Job Name', 'Customer', 'Status', 'Last Execution', 'Retry Count', 'Actions']
const PAGE_SIZE = 10

// Only these statuses can be retried (backend enforces the same rule)
const RETRYABLE = new Set(['Failed'])

const formatDate = (iso) =>
  iso
    ? new Date(iso).toLocaleDateString('en-GB', { dateStyle: 'medium' })
    : '—'

const STATUS_STYLES = {
  Success:           'bg-green-100 text-green-700',
  Failed:            'bg-red-100 text-red-700',
  PermanentlyFailed: 'bg-red-200 text-red-800',
  Running:           'bg-blue-100 text-blue-700',
  Pending:           'bg-yellow-100 text-yellow-700',
}

const statusStyle = (s) => STATUS_STYLES[s] ?? 'bg-gray-100 text-gray-600'

export default function IntegrationJobsTable({ rows = [] }) {
  const [page, setPage]       = useState(1)
  // localRows mirrors props but lets us update status/retryCount after a retry
  const [localRows, setLocalRows] = useState(rows)
  // Set of job IDs currently awaiting a retry response
  const [retrying, setRetrying]   = useState(new Set())

  // Keep localRows in sync when the parent re-fetches and passes new props
  useEffect(() => { setLocalRows(rows) }, [rows])

  const totalPages = Math.max(1, Math.ceil(localRows.length / PAGE_SIZE))
  const start      = (page - 1) * PAGE_SIZE
  const pageRows   = localRows.slice(start, start + PAGE_SIZE)

  async function handleRetry(id) {
    setRetrying(prev => new Set(prev).add(id))
    try {
      const result = await retryJob(id)
      // Update the matching row in-place with the new status + retryCount
      setLocalRows(prev =>
        prev.map(r =>
          r.id === id
            ? { ...r, status: result.jobStatus, retryCount: result.retryCount }
            : r
        )
      )
    } catch (err) {
      console.error('Retry failed:', err)
    } finally {
      setRetrying(prev => { const s = new Set(prev); s.delete(id); return s })
    }
  }

  return (
    <div className="w-full flex flex-col overflow-hidden rounded-[5px] border border-gray-200">

      {/* Table */}
      <table className="w-full border-collapse text-xs">
        <thead>
          <tr className="bg-gray-300">
            {COLUMNS.map((col, i) => (
              <th
                key={i}
                className="text-left text-gray-800 font-semibold px-4 py-3 border-b border-gray-200"
                style={{ borderLeft: 'none', borderRight: 'none' }}
              >
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {pageRows.length === 0 ? (
            <tr className="bg-white">
              <td colSpan={6} className="px-4 py-3 text-center text-gray-400">
                No jobs found.
              </td>
            </tr>
          ) : (
            pageRows.map((row, i) => (
              <tr key={row.id ?? i} className="bg-white border-b border-gray-200 last:border-b-0">

                {/* Job Name */}
                <td className="px-4 py-3 text-black font-medium" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {row.jobName}
                </td>

                {/* Customer */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {row.customerName}
                </td>

                {/* Status badge */}
                <td className="px-4 py-3" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${statusStyle(row.status)}`}>
                    {row.status}
                  </span>
                </td>

                {/* Last Execution */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {formatDate(row.lastExecutedAt)}
                </td>

                {/* Retry Count */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {row.retryCount}
                </td>

                {/* Actions */}
                <td className="px-4 py-3" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {RETRYABLE.has(row.status) ? (
                    <button
                      onClick={() => handleRetry(row.id)}
                      disabled={retrying.has(row.id)}
                      className="bg-gray-800 text-white text-xs font-medium px-3 py-1 rounded-[5px] hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {retrying.has(row.id) ? 'Retrying…' : 'Retry'}
                    </button>
                  ) : (
                    <span className="text-gray-300 text-xs">—</span>
                  )}
                </td>

              </tr>
            ))
          )}
        </tbody>
      </table>

      {/* Footer */}
      <div className="bg-white border-t border-gray-200 px-4 py-2 flex flex-row items-center justify-between">

        {/* Left — rows per page label */}
        <span className="text-xs text-gray-500">Rows per page: {PAGE_SIZE}</span>

        {/* Right — pagination controls */}
        <div className="flex flex-row items-center gap-1">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="w-7 h-7 flex items-center justify-center rounded-[5px] border border-gray-200 text-gray-600 text-xs hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            &#60;
          </button>

          <div className="h-7 px-2 flex items-center justify-center rounded-[5px] border border-gray-200 text-xs font-semibold text-gray-800 min-w-[3.5rem]">
            {page} / {totalPages}
          </div>

          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="w-7 h-7 flex items-center justify-center rounded-[5px] border border-gray-200 text-gray-600 text-xs hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            &#62;
          </button>
        </div>

      </div>
    </div>
  )
}
