using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Models;
using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Core.Services;
using InternalDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternalDashboard.Infrastructure.Services;

/// <summary>
/// Orchestrates the full lifecycle of an integration job run or retry.
///
/// Retry rules:
///   - Only jobs with Status = Failed may be retried.
///   - Each retry increments IntegrationJob.RetryCount.
///   - Maximum retries: MaxRetries (3). On the 3rd failed retry the job transitions
///     to PermanentlyFailed and is locked — manual intervention is required.
///   - Every call (run or retry) persists a new JobExecution record.
///
/// Execution flow (ExecuteAsync):
///   1. Fetch job by ID             →  throws if not found
///   2. Set job.Status = Running    →  SaveChanges
///   3. Determine next AttemptNumber (MAX existing + 1)
///   4. Call IFailureSimulationService.Simulate → (outcomeStatus, failureType?)
///   5. Create and persist a new JobExecution record
///   6. Update job.Status (and RetryCount / PermanentlyFailed if needed) → SaveChanges
///   7. Return the JobExecutionDto to the caller
/// </summary>
public sealed class JobService : IJobService
{
    /// <summary>Maximum number of retry attempts before the job is permanently failed.</summary>
    public const int MaxRetries = 3;

    private readonly AppDbContext _db;
    private readonly IFailureSimulationService _simulator;
    private readonly ILogger<JobService> _logger;

    public JobService(
        AppDbContext db,
        IFailureSimulationService simulator,
        ILogger<JobService> logger)
    {
        _db        = db;
        _simulator = simulator;
        _logger    = logger;
    }

    // ── Public interface ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<JobRunResultDto> RunJobAsync(Guid jobId)
    {
        var job = await FetchJobAsync(jobId);

        _logger.LogInformation(
            "JobService.RunJobAsync: starting job '{JobName}' (Id={JobId}).",
            job.JobName, job.Id);

        return await ExecuteAsync(job, isRetry: false);
    }

    /// <inheritdoc />
    public async Task<JobRunResultDto> RetryJobAsync(Guid jobId)
    {
        var job = await FetchJobAsync(jobId);

        // Guard 1 — only Failed jobs may be retried
        if (job.Status != JobStatus.Failed)
            throw new InvalidOperationException(
                $"Job '{job.JobName}' ({jobId}) cannot be retried — " +
                $"current status is '{job.Status}'. Only Failed jobs are retryable.");

        // Guard 2 — must have at least one prior execution to qualify as a retry
        var existingAttempts = await _db.JobExecutions
            .Where(e => e.IntegrationJobId == jobId)
            .CountAsync();

        if (existingAttempts == 0)
            throw new InvalidOperationException(
                $"Job '{job.JobName}' ({jobId}) has no execution history. " +
                $"Use RunJobAsync for the first run.");

        // Guard 3 — enforce the retry cap BEFORE executing.
        //           RetryCount is the number of retries already consumed.
        //           If it equals MaxRetries, the next attempt would exceed the limit.
        if (job.RetryCount >= MaxRetries)
        {
            job.Status = JobStatus.PermanentlyFailed;
            await _db.SaveChangesAsync();

            _logger.LogError(
                "JobService.RetryJobAsync: job '{JobName}' ({JobId}) has exhausted all " +
                "{Max} retry attempts and is now PermanentlyFailed. Manual intervention required.",
                job.JobName, job.Id, MaxRetries);

            throw new InvalidOperationException(
                $"Job '{job.JobName}' ({jobId}) has exhausted all {MaxRetries} retry attempts. " +
                $"The job has been marked as Permanently Failed — manual intervention is required.");
        }

        _logger.LogInformation(
            "JobService.RetryJobAsync: retrying job '{JobName}' (Id={JobId}), " +
            "retry {Next}/{Max}.",
            job.JobName, job.Id, job.RetryCount + 1, MaxRetries);

        return await ExecuteAsync(job, isRetry: true);
    }

    // ── Shared execution pipeline ─────────────────────────────────────────────

    /// <summary>
    /// Core execution steps shared by both RunJobAsync and RetryJobAsync.
    /// The <paramref name="isRetry"/> flag controls whether RetryCount is incremented
    /// and whether PermanentlyFailed escalation is evaluated.
    /// </summary>
    private async Task<JobRunResultDto> ExecuteAsync(IntegrationJob job, bool isRetry)
    {
        // Step 2 — mark job as Running
        job.Status = JobStatus.Running;
        await _db.SaveChangesAsync();

        // Step 3 — determine next AttemptNumber
        var lastAttempt = await _db.JobExecutions
            .Where(e => e.IntegrationJobId == job.Id)
            .OrderByDescending(e => e.AttemptNumber)
            .Select(e => (int?)e.AttemptNumber)
            .FirstOrDefaultAsync() ?? 0;

        var nextAttempt = lastAttempt + 1;

        _logger.LogInformation(
            "JobService.ExecuteAsync: job '{JobName}' — attempt #{Attempt}.",
            job.JobName, nextAttempt);

        // Step 4 — call the failure simulation service
        var (outcomeStatus, failureType) = _simulator.Simulate(job.Id, nextAttempt);

        // Step 5 — create and persist the new JobExecution record
        var execution = new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = job.Id,
            AttemptNumber    = nextAttempt,
            Status           = outcomeStatus,
            FailureType      = failureType,
            ExecutedAt       = DateTime.UtcNow,
        };
        await _db.JobExecutions.AddAsync(execution);

        // Step 6 — update job status and (for retries) the retry counter
        if (isRetry)
        {
            job.RetryCount++;   // always increment so the count is accurate

            if (outcomeStatus == JobStatus.Failed && job.RetryCount >= MaxRetries)
            {
                // This was the final allowed retry and it still failed → lock permanently
                job.Status = JobStatus.PermanentlyFailed;

                _logger.LogError(
                    "JobService.ExecuteAsync: job '{JobName}' ({JobId}) failed on retry " +
                    "{RetryCount}/{Max} — now PermanentlyFailed. Manual intervention required.",
                    job.JobName, job.Id, job.RetryCount, MaxRetries);
            }
            else
            {
                job.Status = outcomeStatus;
            }
        }
        else
        {
            job.Status = outcomeStatus;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "JobService.ExecuteAsync: job '{JobName}' attempt #{Attempt} → {Status}{Failure}.",
            job.JobName,
            nextAttempt,
            job.Status,
            failureType is not null ? $" ({failureType})" : string.Empty);

        // Step 7 — project to DTOs and return the combined result to the caller
        var executionDto = new JobExecutionDto(
            execution.Id,
            execution.AttemptNumber,
            execution.Status.ToString(),
            execution.FailureType?.ToString(),
            execution.ExecutedAt);

        int retriesRemaining = Math.Max(0, MaxRetries - job.RetryCount);

        return new JobRunResultDto(
            JobId:            job.Id,
            JobStatus:        job.Status.ToString(),
            RetryCount:       job.RetryCount,
            RetriesRemaining: retriesRemaining,
            Execution:        executionDto);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches an IntegrationJob by ID.
    /// Throws <see cref="InvalidOperationException"/> with "not found" when the job
    /// does not exist — GlobalExceptionMiddleware maps this to HTTP 404.
    /// </summary>
    private async Task<IntegrationJob> FetchJobAsync(Guid jobId)
        => await _db.IntegrationJobs
               .FirstOrDefaultAsync(j => j.Id == jobId)
           ?? throw new InvalidOperationException(
               $"IntegrationJob '{jobId}' was not found.");
}
