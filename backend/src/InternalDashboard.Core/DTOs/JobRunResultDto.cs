namespace InternalDashboard.Core.DTOs;

/// <summary>
/// Returned by POST /api/jobs/{id}/run  and  POST /api/jobs/{id}/retry.
///
/// Bundles the new JobExecution record with the updated job state so the caller
/// has everything it needs in a single response — no follow-up GET required.
/// </summary>
public sealed record JobRunResultDto(
    /// <summary>The job ID that was run or retried.</summary>
    Guid   JobId,

    /// <summary>Final status of the job after this run (e.g. Success, Failed, PermanentlyFailed).</summary>
    string JobStatus,

    /// <summary>
    /// Number of retries consumed so far (0 for a fresh run).
    /// Counts only RetryJobAsync calls, not the initial RunJobAsync.
    /// </summary>
    int    RetryCount,

    /// <summary>
    /// How many more retries are allowed before the job becomes PermanentlyFailed.
    /// Zero when the job is already PermanentlyFailed or has exhausted all attempts.
    /// </summary>
    int    RetriesRemaining,

    /// <summary>The execution record created by this run or retry.</summary>
    JobExecutionDto Execution
);
