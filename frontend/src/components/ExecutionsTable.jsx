import { useState } from 'react'

/**
 * ExecutionsTable
 *
 * Props:
 *   rows  {Array<{ executionId, jobId, jobName, customerName, status, failureType, attemptNumber, executedAt }>}
 *
 * Columns: Job Name | Customer | Status | Failure Type | Attempt # | Executed At
 */

const COLUMNS = ['Job Name', 'Customer', 'Status', 'Failure Type', 'Attempt #', 'Executed At']
const PAGE_SIZE = 10

const formatDate = (iso) =>
  iso
    ? new Date(iso).toLocaleString('en-GB', {
        dateStyle: 'medium',
        timeStyle: 'short',
      })
    : '—'

const STATUS_STYLES = {
  Success:           'bg-green-100 text-green-700',
  Failed:            'bg-red-100 text-red-700',
  PermanentlyFailed: 'bg-red-200 text-red-800',
  Running:           'bg-blue-100 text-blue-700',
  Pending:           'bg-yellow-100 text-yellow-700',
}

const statusStyle = (s) => STATUS_STYLES[s] ?? 'bg-gray-100 text-gray-600'

export default function ExecutionsTable({ rows = [] }) {
  const [page, setPage] = useState(1)

  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE))
  const start      = (page - 1) * PAGE_SIZE
  const pageRows   = rows.slice(start, start + PAGE_SIZE)

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
                No executions found.
              </td>
            </tr>
          ) : (
            pageRows.map((row, i) => (
              <tr key={row.executionId ?? i} className="bg-white border-b border-gray-200 last:border-b-0">

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

                {/* Failure Type */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {row.failureType ?? '—'}
                </td>

                {/* Attempt Number */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {row.attemptNumber}
                </td>

                {/* Executed At */}
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  {formatDate(row.executedAt)}
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
