using InternalDashboard.Core.DTOs;

namespace InternalDashboard.Core.Services;

/// <summary>
/// Orchestration contract for the integration job lifecycle.
/// This service handles high-level job operations only.
/// Data access is the responsibility of the Infrastructure layer.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Fetches the job, runs the failure simulation, persists a JobExecution record,
    /// updates the job status, and returns a <see cref="JobRunResultDto"/> containing
    /// both the execution record and the updated job state.
    /// </summary>
    /// <param name="jobId">The unique identifier of the IntegrationJob to run.</param>
    /// <returns>
    /// A <see cref="JobRunResultDto"/> with the new execution, final job status,
    /// retry count and retries remaining.
    /// </returns>
    Task<JobRunResultDto> RunJobAsync(Guid jobId);

    /// <summary>
    /// Retries the specified integration job after a previous failure.
    /// Only jobs with Status = Failed and RetryCount &lt; MaxRetries may be retried.
    /// </summary>
    /// <param name="jobId">The unique identifier of the IntegrationJob to retry.</param>
    /// <returns>
    /// A <see cref="JobRunResultDto"/> with the new execution, final job status,
    /// updated retry count and retries remaining.
    /// </returns>
    Task<JobRunResultDto> RetryJobAsync(Guid jobId);
}
