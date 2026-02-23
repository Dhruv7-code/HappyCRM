using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InternalDashboard.Tests;

/// <summary>
/// Unit tests for FailureSimulationService.
///
/// Because ResolveFailureType is deterministic (hash → FailureType),
/// we test it directly with hand-crafted hash values that hit each rule.
///
/// Rule priority (evaluated top to bottom):
///   hash % 3  == 0  →  Timeout
///   hash % 7  == 0  →  RateLimitExceeded
///   hash % 11 == 0  →  NetworkError
///   hash % 13 == 0  →  ThirdPartyServiceDown
///   hash % 5  == 0  →  DataValidationError
///   none matched    →  null (Success)
///
/// Note: AuthenticationError has no reachable hash rule — every hash%15==9
/// value is also hash%3==0, so Timeout always wins. AuthError exists only in
/// seeded/manual JobExecution data.
/// </summary>
[TestFixture]
public class FailureSimulationServiceTests
{
    // ── ResolveFailureType — each rule branch ─────────────────────────────────

    [Test]
    [TestCase(3,   FailureType.Timeout,              Description = "hash % 3 == 0")]
    [TestCase(6,   FailureType.Timeout,              Description = "hash % 3 == 0 (multiple of 3)")]
    [TestCase(9,   FailureType.Timeout,              Description = "hash % 3 == 0 (also hash%15==9, but Timeout checked first)")]
    [TestCase(7,   FailureType.RateLimitExceeded,    Description = "hash % 7 == 0")]
    [TestCase(49,  FailureType.RateLimitExceeded,    Description = "hash % 7 == 0")]
    [TestCase(11,  FailureType.NetworkError,          Description = "hash % 11 == 0")]
    [TestCase(121, FailureType.NetworkError,          Description = "hash % 11 == 0")]
    [TestCase(13,  FailureType.ThirdPartyServiceDown, Description = "hash % 13 == 0")]
    [TestCase(169, FailureType.ThirdPartyServiceDown, Description = "hash % 13 == 0")]
    [TestCase(5,   FailureType.DataValidationError,   Description = "hash % 5 == 0")]
    [TestCase(25,  FailureType.DataValidationError,   Description = "hash % 5 == 0")]
    public void ResolveFailureType_WithMatchingHash_ReturnsExpectedFailureType(
        int hash, FailureType expected)
    {
        var result = FailureSimulationService.ResolveFailureType(hash);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(1,  Description = "1  — matches no rule")]
    [TestCase(2,  Description = "2  — matches no rule")]
    [TestCase(4,  Description = "4  — matches no rule")]
    [TestCase(8,  Description = "8  — matches no rule")]
    [TestCase(16, Description = "16 — matches no rule")]
    [TestCase(17, Description = "17 — matches no rule")]
    [TestCase(19, Description = "19 — matches no rule")]
    [TestCase(23, Description = "23 — matches no rule")]
    public void ResolveFailureType_WithNoMatchingHash_ReturnsNull(int hash)
    {
        var result = FailureSimulationService.ResolveFailureType(hash);
        Assert.That(result, Is.Null);
    }

    // ── Priority order — higher-priority rule wins when multiple match ────────

    [Test]
    public void ResolveFailureType_WhenHashMatchesBothTimeoutAndRateLimit_TimeoutWins()
    {
        // 21 % 3 == 0 (Timeout) AND 21 % 7 == 0 (RateLimitExceeded)
        // Timeout rule is checked first — it must win
        const int hash = 21;
        var result = FailureSimulationService.ResolveFailureType(hash);
        Assert.That(result, Is.EqualTo(FailureType.Timeout));
    }

    [Test]
    public void ResolveFailureType_WhenHashMatchesBothRateLimitAndNetwork_RateLimitWins()
    {
        // 77 % 7 == 0 (RateLimitExceeded) AND 77 % 11 == 0 (NetworkError)
        const int hash = 77;
        var result = FailureSimulationService.ResolveFailureType(hash);
        Assert.That(result, Is.EqualTo(FailureType.RateLimitExceeded));
    }

    // ── Simulate() — full service through NullLogger ──────────────────────────

    [Test]
    public void Simulate_WithJobIdThatHashesToMultipleOf3_ReturnsFailed_WithTimeout()
    {
        // Find a Guid whose Math.Abs(GetHashCode()) % 3 == 0
        // We brute-force one here deterministically
        var sut = new FailureSimulationService(NullLogger<FailureSimulationService>.Instance);

        Guid? targetId = null;
        for (int i = 1; i <= 10_000; i++)
        {
            var candidate = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            int h = Math.Abs(candidate.GetHashCode() == int.MinValue ? 0 : candidate.GetHashCode());
            if (h % 3 == 0) { targetId = candidate; break; }
        }

        Assume.That(targetId.HasValue, "Could not find a suitable Guid in range — extend search.");

        var (status, failureType) = sut.Simulate(targetId!.Value, attemptNumber: 1);

        Assert.Multiple(() =>
        {
            Assert.That(status,      Is.EqualTo(JobStatus.Failed));
            Assert.That(failureType, Is.EqualTo(FailureType.Timeout));
        });
    }

    [Test]
    public void Simulate_WithJobIdThatHashesToNoRule_ReturnsSuccess()
    {
        var sut = new FailureSimulationService(NullLogger<FailureSimulationService>.Instance);

        // Brute-force a Guid whose hash matches no rule
        Guid? targetId = null;
        for (int i = 1; i <= 10_000; i++)
        {
            var candidate = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            int h = Math.Abs(candidate.GetHashCode() == int.MinValue ? 0 : candidate.GetHashCode());
            if (h % 3 != 0 && h % 7 != 0 && h % 11 != 0 &&
                h % 13 != 0 && h % 5 != 0 && h % 15 != 9)
            {
                targetId = candidate;
                break;
            }
        }

        Assume.That(targetId.HasValue, "Could not find a suitable Guid in range — extend search.");

        var (status, failureType) = sut.Simulate(targetId!.Value, attemptNumber: 1);

        Assert.Multiple(() =>
        {
            Assert.That(status,      Is.EqualTo(JobStatus.Success));
            Assert.That(failureType, Is.Null);
        });
    }

    // ── Edge case: int.MinValue hash ──────────────────────────────────────────

    [Test]
    public void ResolveFailureType_WithHashZero_ReturnTimeout()
    {
        // 0 % 3 == 0 → Timeout (also the fallback for int.MinValue overflow)
        var result = FailureSimulationService.ResolveFailureType(0);
        Assert.That(result, Is.EqualTo(FailureType.Timeout));
    }
}
