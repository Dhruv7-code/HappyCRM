using InternalDashboard.Core.Models.Enums;

namespace InternalDashboard.Core.Services;

/// <summary>
/// Simulates the outcome of running an integration job.
/// Returns whether the job succeeded and, if not, what type of failure occurred.
/// Real external call logic will replace this later.
/// </summary>
public interface IFailureSimulationService
{
    /// <summary>
    /// Simulates executing a job and returns the outcome.
    /// </summary>
    /// <param name="jobId">The job being executed.</param>
    /// <param name="attemptNumber">The current attempt number (used to weight retry success).</param>
    /// <returns>
    /// A tuple of:
    ///   - <c>status</c>: <see cref="JobStatus.Success"/> or <see cref="JobStatus.Failed"/>
    ///   - <c>failureType</c>: the failure reason, or <c>null</c> on success
    /// </returns>
    (JobStatus status, FailureType? failureType) Simulate(Guid jobId, int attemptNumber);
}
