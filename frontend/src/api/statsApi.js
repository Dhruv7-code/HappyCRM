import { get } from './client.js'

/**
 * Fetch success count + percentage.
 * GET /api/stats/jobs/success
 * @returns {Promise<{ count: number, percentage: number }>}
 */
export function getSuccessStats() {
  return get('/api/stats/jobs/success')
}

/**
 * Fetch failed count + percentage.
 * GET /api/stats/jobs/failed
 * @returns {Promise<{ count: number, percentage: number }>}
 */
export function getFailedStats() {
  return get('/api/stats/jobs/failed')
}

/**
 * Fetch pending count + percentage.
 * GET /api/stats/jobs/pending
 * @returns {Promise<{ count: number, percentage: number }>}
 */
export function getPendingStats() {
  return get('/api/stats/jobs/pending')
}
