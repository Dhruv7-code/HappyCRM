import { get, post, put } from './client.js'

// ── Customer queries ───────────────────────────────────────────────────────────

/**
 * GET /api/customers
 * Returns CustomerListDto[] — each customer with their active job count.
 */
export const getAllCustomers = () => get('/api/customers')

/**
 * GET /api/dashboard/customers
 * Returns CustomerDto[] — used for total-count stat card.
 */
export const getCustomers = () => get('/api/dashboard/customers')

/**
 * Convenience: fetch all customers and return only the total count.
 */
export const getTotalCustomers = async () => {
  const customers = await getCustomers()
  return customers.length
}

// ── Customer mutations ─────────────────────────────────────────────────────────

/**
 * POST /api/customers/newcustomer
 * Body: { name, email }
 * Returns CreateCustomerResponse: { id, name, email, createdAt }
 */
export const addNewCustomer = (customer) =>
  post('/api/customers/newcustomer', customer)

/**
 * PUT /api/customers/{id}/updateuser
 * Body: { name, email }
 * Returns UpdateCustomerResponse: { id, name, email, createdAt }
 */
export const updateCustomer = (id, customer) =>
  put(`/api/customers/${id}/updateuser`, customer)
