import { get, post } from './client.js'

// ── Integration Jobs ───────────────────────────────────────────────────────────

/**
 * GET /api/dashboard/jobs
 * Returns IntegrationJobDto[] — all jobs with last execution summary.
 */
export const getIntegrationJobs = () => get('/api/dashboard/jobs')

/**
 * POST /api/jobs/{id}/retry
 * Returns JobRunResultDto: { jobId, jobStatus, retryCount, retriesRemaining, execution }
 */
export const retryJob = (id) => post(`/api/jobs/${id}/retry`)

// ── Executions ────────────────────────────────────────────────────────────────

/**
 * GET /api/dashboard/recent-executions
 * Returns the 5 most recent RecentJobExecutionDto[] ordered by ExecutedAt DESC.
 */
export const getRecentJobExecutions = () => get('/api/dashboard/recent-executions')

/**
 * GET /api/dashboard/executions
 * Returns ExecutionListDto[] — all executions ordered by ExecutedAt DESC.
 */
export const getAllExecutions = () => get('/api/dashboard/executions')
