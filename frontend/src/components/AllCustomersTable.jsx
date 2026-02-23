import { useState, useEffect } from 'react'
import EditCustomerForm from './EditCustomerForm'

/**
 * AllCustomersTable
 *
 * Props:
 *   rows  {Array<{ id, name, email, createdAt, totalJobs }>}
 *
 * Columns: Name | Email | Created At | Total Jobs | (blank) | Actions
 * Footer:  "Rows per page: 5"  |  < [page] >
 */

const COLUMNS = ['Name', 'Email', 'Created At', 'Active Jobs', '', 'Actions']
const PAGE_SIZE = 5

const formatDate = (iso) =>
  new Date(iso).toLocaleDateString('en-GB', { dateStyle: 'medium' })

export default function AllCustomersTable({ rows = [] }) {
  const [page, setPage]                       = useState(1)
  const [localRows, setLocalRows]             = useState(rows)
  const [editingCustomer, setEditingCustomer] = useState(null)

  // Keep localRows in sync when the parent re-fetches and passes new props
  useEffect(() => { setLocalRows(rows) }, [rows])

  const totalPages = Math.max(1, Math.ceil(localRows.length / PAGE_SIZE))
  const start      = (page - 1) * PAGE_SIZE
  const pageRows   = localRows.slice(start, start + PAGE_SIZE)

  function handleUpdated(updated) {
    setLocalRows(prev =>
      prev.map(r => r.id === updated.id ? { ...r, name: updated.name, email: updated.email } : r)
    )
    setEditingCustomer(null)
  }

  return (
    <div className="w-full flex flex-col overflow-hidden rounded-[5px] border border-gray-200 relative">

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
                No customers found.
              </td>
            </tr>
          ) : (
            pageRows.map((row, i) => (
              <tr key={row.id ?? i} className="bg-white border-b border-gray-200 last:border-b-0">
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.name}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.email}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{formatDate(row.createdAt)}</td>
                <td className="px-4 py-3 text-black" style={{ borderLeft: 'none', borderRight: 'none' }}>{row.activeJobs}</td>
                {/* Blank column — no value */}
                <td className="px-4 py-3" style={{ borderLeft: 'none', borderRight: 'none' }} />
                {/* Actions */}
                <td className="px-4 py-3" style={{ borderLeft: 'none', borderRight: 'none' }}>
                  <button
                    onClick={() => setEditingCustomer(row)}
                    className="bg-gray-800 text-white text-xs font-medium px-3 py-1 rounded-[5px] hover:bg-gray-700 transition-colors"
                  >
                    Edit
                  </button>
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

          <div className="w-7 h-7 flex items-center justify-center rounded-[5px] border border-gray-200 text-xs font-semibold text-gray-800">
            {page}
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

      {/* ── Slide-in panel backdrop ── */}
      {editingCustomer && (
        <div
          className="fixed inset-0 bg-black/30 z-40"
          onClick={() => setEditingCustomer(null)}
        />
      )}

      {/* ── Slide-in panel ── */}
      <div
        className={`fixed top-0 right-0 h-full w-80 bg-white shadow-2xl z-50 flex flex-col transition-transform duration-300 ease-in-out ${
          editingCustomer ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {/* Panel header */}
        <div className="flex items-center justify-between px-6 py-5 border-b border-gray-200">
          <h2 className="text-sm font-bold text-gray-800">Edit Customer</h2>
          <button
            onClick={() => setEditingCustomer(null)}
            className="text-gray-400 hover:text-gray-700 text-lg leading-none transition-colors"
          >
            ✕
          </button>
        </div>

        {/* Panel body */}
        <div className="flex-1 overflow-y-auto px-6 py-5">
          {editingCustomer && (
            <EditCustomerForm
              customer={editingCustomer}
              onSuccess={handleUpdated}
            />
          )}
        </div>
      </div>

    </div>
  )
}
