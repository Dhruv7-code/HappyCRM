import { useState } from 'react'
import { updateCustomer } from '../api/customersApi'

/**
 * EditCustomerForm
 *
 * Props:
 *   customer  {{ id, name, email }}  — the row being edited (pre-populates the fields)
 *   onSuccess(updatedCustomer)       — called after a successful update
 */
export default function EditCustomerForm({ customer, onSuccess }) {
  const [name, setName]       = useState(customer?.name  ?? '')
  const [email, setEmail]     = useState(customer?.email ?? '')
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(null)  // success message string
  const [error, setError]     = useState(null)  // error message string

  async function handleSubmit(e) {
    e.preventDefault()
    setSuccess(null)
    setError(null)
    setLoading(true)

    try {
      const updated = await updateCustomer(customer.id, { name, email })
      setSuccess(`Customer "${updated.name}" updated successfully.`)
      onSuccess?.(updated)
    } catch (err) {
      const raw   = err.message ?? ''
      const match = raw.match(/\d{3}: (.+)$/)
      setError(match ? match[1] : raw || 'Something went wrong. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">

      {/* Success banner */}
      {success && (
        <div className="text-xs font-medium text-green-700 bg-green-50 border border-green-200 rounded-[5px] px-3 py-2">
          ✓ {success}
        </div>
      )}

      {/* Error banner */}
      {error && (
        <div className="text-xs font-medium text-red-700 bg-red-50 border border-red-200 rounded-[5px] px-3 py-2">
          ✕ {error}
        </div>
      )}

      {/* Name */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-semibold text-gray-700" htmlFor="edit-customer-name">
          Name
        </label>
        <input
          id="edit-customer-name"
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder="Acme Corp"
          required
          className="text-sm border border-gray-300 rounded-[5px] px-3 py-2 text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-gray-400"
        />
      </div>

      {/* Email */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-semibold text-gray-700" htmlFor="edit-customer-email">
          Email
        </label>
        <input
          id="edit-customer-email"
          type="email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          placeholder="contact@acme.com"
          required
          className="text-sm border border-gray-300 rounded-[5px] px-3 py-2 text-gray-800 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-gray-400"
        />
      </div>

      {/* Submit */}
      <button
        type="submit"
        disabled={loading}
        className="self-end bg-gray-800 text-white text-xs font-medium px-4 py-2 rounded-[5px] hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {loading ? 'Saving…' : 'Save Changes'}
      </button>

    </form>
  )
}
