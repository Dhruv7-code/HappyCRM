using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Core.Services;
using Microsoft.Extensions.Logging;

namespace InternalDashboard.Infrastructure.Services;

/// <summary>
/// Deterministic failure simulation based on the job ID's hash code.
///
/// The hash code is derived from the jobId so the same job always produces
/// the same outcome — making results predictable, testable and defensive.
///
/// Failure type resolution (evaluated in priority order):
///   hash % 3  == 0  →  Timeout
///   hash % 7  == 0  →  RateLimitExceeded
///   hash % 11 == 0  →  NetworkError
///   hash % 13 == 0  →  ThirdPartyServiceDown
///   hash % 5  == 0  →  DataValidationError
///   none matched    →  Success
///
/// Note: An AuthenticationError branch via "hash % 15 == 9" is mathematically
/// unreachable — every value satisfying hash%15==9 is also divisible by 3
/// (15k+9 = 3(5k+3)), so the Timeout rule always fires first.
/// AuthenticationError can still appear in seeded/manual JobExecution records.
/// </summary>
public sealed class FailureSimulationService : IFailureSimulationService
{
    private readonly ILogger<FailureSimulationService> _logger;

    public FailureSimulationService(ILogger<FailureSimulationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public (JobStatus status, FailureType? failureType) Simulate(Guid jobId, int attemptNumber)
    {
        // Derive a stable, non-negative integer from the job ID.
        // Math.Abs guard prevents int.MinValue overflow edge-case.
        int hash = Math.Abs(jobId.GetHashCode() == int.MinValue
            ? 0
            : jobId.GetHashCode());

        var failureType = ResolveFailureType(hash);

        if (failureType is null)
        {
            _logger.LogInformation(
                "FailureSimulationService: job {JobId} (hash={Hash}) attempt #{Attempt} → Success.",
                jobId, hash, attemptNumber);

            return (JobStatus.Success, null);
        }

        _logger.LogWarning(
            "FailureSimulationService: job {JobId} (hash={Hash}) attempt #{Attempt} → Failed ({FailureType}).",
            jobId, hash, attemptNumber, failureType);

        return (JobStatus.Failed, failureType);
    }

    /// <summary>
    /// Maps a hash value to a FailureType using modulo rules (evaluated in priority order).
    /// Returns null when no rule matches, indicating a successful outcome.
    /// </summary>
    internal static FailureType? ResolveFailureType(int hash)
    {
        if (hash % 3  == 0) return FailureType.Timeout;
        if (hash % 7  == 0) return FailureType.RateLimitExceeded;
        if (hash % 11 == 0) return FailureType.NetworkError;
        if (hash % 13 == 0) return FailureType.ThirdPartyServiceDown;
        if (hash % 5  == 0) return FailureType.DataValidationError;
        // Note: AuthenticationError has no reachable hash rule (hash%15==9 is always
        // also hash%3==0, so Timeout wins). It remains in FailureType for seeded data.
        return null; // no rule matched → Success
    }
}
