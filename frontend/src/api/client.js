/**
 * client.js
 *
 * Thin fetch wrapper — all API modules use this instead of calling fetch() directly.
 * Base URL is read from the Vite env variable VITE_API_URL (default: http://localhost:5050).
 */

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5050'

/**
 * @param {string} path   — e.g. '/api/dashboard/stats'
 * @param {RequestInit} [options]
 * @returns {Promise<any>}
 */
async function request(path, options = {}) {
  const url = `${BASE_URL}${path}`

  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`${options.method ?? 'GET'} ${path} → ${response.status}: ${text}`)
  }

  // 204 No Content — return null instead of trying to parse empty body
  if (response.status === 204) return null

  return response.json()
}

export const get  = (path, options)       => request(path, { ...options, method: 'GET'  })
export const post = (path, body, options) => request(path, { ...options, method: 'POST', body: JSON.stringify(body) })
export const put  = (path, body, options) => request(path, { ...options, method: 'PUT',  body: JSON.stringify(body) })
