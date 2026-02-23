using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Models;
using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Core.Services;
using InternalDashboard.Infrastructure.Data;
using InternalDashboard.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InternalDashboard.Tests;

/// <summary>
/// Unit tests for JobService covering:
///   - RunJobAsync  → Success case
///   - RunJobAsync  → Failure case
///   - RetryJobAsync → retries a Failed job → Success or Failure
///   - RetryJobAsync → guard: rejects non-Failed jobs
///   - RetryJobAsync → guard: rejects jobs with no execution history
/// </summary>
[TestFixture]
public class JobServiceTests
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fresh in-memory AppDbContext with a unique DB name per test
    /// so tests never share state.
    /// </summary>
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds one Customer + one IntegrationJob into the given context and returns both.
    /// </summary>
    private static async Task<(Customer customer, IntegrationJob job)> SeedJobAsync(
        AppDbContext db,
        JobStatus initialStatus = JobStatus.Pending)
    {
        var customer = new Customer
        {
            Id        = Guid.NewGuid(),
            Name      = "Acme Corp",
            Email     = "admin@acme.com",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        var job = new IntegrationJob
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            JobName    = "Sync CRM Contacts",
            Status     = initialStatus,
            CreatedAt  = DateTime.UtcNow.AddDays(-1),
        };

        db.Customers.Add(customer);
        db.IntegrationJobs.Add(job);
        await db.SaveChangesAsync();

        return (customer, job);
    }

    /// <summary>Builds a JobService with a simulator that always returns the given outcome.</summary>
    private static JobService BuildService(
        AppDbContext db,
        JobStatus simulatedStatus,
        FailureType? simulatedFailureType = null)
    {
        var simulator = Substitute.For<IFailureSimulationService>();
        simulator
            .Simulate(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns((simulatedStatus, simulatedFailureType));

        return new JobService(db, simulator, NullLogger<JobService>.Instance);
    }

    // ── RunJobAsync — Success ─────────────────────────────────────────────────

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsSuccess_JobStatusBecomesSuccess()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Success);

        // Act
        await sut.RunJobAsync(job.Id);

        // Assert — job status updated to Success
        var updatedJob = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updatedJob!.Status, Is.EqualTo(JobStatus.Success));
    }

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsSuccess_CreatesOneExecutionWithStatusSuccess()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Success);

        // Act
        await sut.RunJobAsync(job.Id);

        // Assert — exactly one execution record, attempt #1, no failure type
        var executions = await db.JobExecutions
            .Where(e => e.IntegrationJobId == job.Id)
            .ToListAsync();

        Assert.That(executions, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(executions[0].AttemptNumber, Is.EqualTo(1));
            Assert.That(executions[0].Status,        Is.EqualTo(JobStatus.Success));
            Assert.That(executions[0].FailureType,   Is.Null);
        });
    }

    // ── RunJobAsync — Failure ─────────────────────────────────────────────────

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsFailed_JobStatusBecomesFailed()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act
        await sut.RunJobAsync(job.Id);

        // Assert — job status updated to Failed
        var updatedJob = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updatedJob!.Status, Is.EqualTo(JobStatus.Failed));
    }

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsFailed_CreatesOneExecutionWithFailureType()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act
        await sut.RunJobAsync(job.Id);

        // Assert — execution records the correct failure type
        var executions = await db.JobExecutions
            .Where(e => e.IntegrationJobId == job.Id)
            .ToListAsync();

        Assert.That(executions, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(executions[0].AttemptNumber, Is.EqualTo(1));
            Assert.That(executions[0].Status,        Is.EqualTo(JobStatus.Failed));
            Assert.That(executions[0].FailureType,   Is.EqualTo(FailureType.Timeout));
        });
    }

    // ── RetryJobAsync — retries a failed job ──────────────────────────────────

    [Test]
    public async Task RetryJobAsync_WhenJobIsFailed_CreatesExecutionWithIncrementedAttemptNumber()
    {
        // Arrange — job already has one failed execution (attempt #1)
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);

        db.JobExecutions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = job.Id,
            AttemptNumber    = 1,
            Status           = JobStatus.Failed,
            FailureType      = FailureType.Timeout,
            ExecutedAt       = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, JobStatus.Success);

        // Act
        await sut.RetryJobAsync(job.Id);

        // Assert — retry creates attempt #2
        var executions = await db.JobExecutions
            .Where(e => e.IntegrationJobId == job.Id)
            .OrderBy(e => e.AttemptNumber)
            .ToListAsync();

        Assert.That(executions, Has.Count.EqualTo(2));
        Assert.That(executions[1].AttemptNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task RetryJobAsync_WhenSimulatorReturnsSuccess_JobStatusBecomesSuccess()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);

        db.JobExecutions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = job.Id,
            AttemptNumber    = 1,
            Status           = JobStatus.Failed,
            FailureType      = FailureType.AuthenticationError,
            ExecutedAt       = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, JobStatus.Success);

        // Act
        await sut.RetryJobAsync(job.Id);

        // Assert
        var updatedJob = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updatedJob!.Status, Is.EqualTo(JobStatus.Success));
    }

    [Test]
    public async Task RetryJobAsync_WhenSimulatorReturnsFailed_JobStatusRemainesFailed()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);

        db.JobExecutions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = job.Id,
            AttemptNumber    = 1,
            Status           = JobStatus.Failed,
            FailureType      = FailureType.RateLimitExceeded,
            ExecutedAt       = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, JobStatus.Failed, FailureType.NetworkError);

        // Act
        await sut.RetryJobAsync(job.Id);

        // Assert
        var updatedJob = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updatedJob!.Status, Is.EqualTo(JobStatus.Failed));
    }

    // ── RetryJobAsync — guard rails ───────────────────────────────────────────

    [Test]
    public async Task RetryJobAsync_WhenJobIsNotFailed_ThrowsInvalidOperationException()
    {
        // Arrange — job is Success, not Failed
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Success);
        var sut = BuildService(db, JobStatus.Success);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RetryJobAsync(job.Id));

        Assert.That(ex!.Message, Does.Contain("retried"));
    }

    [Test]
    public async Task RetryJobAsync_WhenJobHasNoExecutionHistory_ThrowsInvalidOperationException()
    {
        // Arrange — job is marked Failed but has no JobExecution rows
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);
        var sut = BuildService(db, JobStatus.Success);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RetryJobAsync(job.Id));

        Assert.That(ex!.Message, Does.Contain("no execution history"));
    }

    [Test]
    public async Task RunJobAsync_WhenJobDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange — random ID that doesn't exist in DB
        await using var db = CreateDb();
        var sut = BuildService(db, JobStatus.Success);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RunJobAsync(Guid.NewGuid()));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    // ── RunJobAsync — returned JobRunResultDto ────────────────────────────────

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsSuccess_ReturnsDtoWithCorrectFields()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Success);

        // Act
        JobRunResultDto result = await sut.RunJobAsync(job.Id);

        // Assert — top-level job state
        Assert.Multiple(() =>
        {
            Assert.That(result.JobId,            Is.EqualTo(job.Id));
            Assert.That(result.JobStatus,        Is.EqualTo("Success"));
            Assert.That(result.RetryCount,       Is.EqualTo(0));
            Assert.That(result.RetriesRemaining, Is.EqualTo(JobService.MaxRetries));
        });

        // Assert — nested execution record
        Assert.Multiple(() =>
        {
            Assert.That(result.Execution.Id,            Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Execution.AttemptNumber, Is.EqualTo(1));
            Assert.That(result.Execution.Status,        Is.EqualTo("Success"));
            Assert.That(result.Execution.FailureType,   Is.Null);
            Assert.That(result.Execution.ExecutedAt,    Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        });
    }

    [Test]
    public async Task RunJobAsync_WhenSimulatorReturnsFailed_ReturnsDtoWithFailureType()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act
        JobRunResultDto result = await sut.RunJobAsync(job.Id);

        // Assert — job state reflects the failure
        Assert.Multiple(() =>
        {
            Assert.That(result.JobStatus,        Is.EqualTo("Failed"));
            Assert.That(result.RetryCount,       Is.EqualTo(0));
            Assert.That(result.RetriesRemaining, Is.EqualTo(JobService.MaxRetries));
        });

        // Assert — execution captures the failure type
        Assert.Multiple(() =>
        {
            Assert.That(result.Execution.AttemptNumber, Is.EqualTo(1));
            Assert.That(result.Execution.Status,        Is.EqualTo("Failed"));
            Assert.That(result.Execution.FailureType,   Is.EqualTo("Timeout"));
        });
    }

    [Test]
    public async Task RunJobAsync_ReturnedExecutionId_MatchesPersistedExecutionId()
    {
        // Arrange
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db);
        var sut = BuildService(db, JobStatus.Success);

        // Act
        JobRunResultDto result = await sut.RunJobAsync(job.Id);

        // Assert — the execution ID in the DTO matches the row written to the DB
        var persisted = await db.JobExecutions.FindAsync(result.Execution.Id);
        Assert.That(persisted,                    Is.Not.Null);
        Assert.That(persisted!.AttemptNumber, Is.EqualTo(result.Execution.AttemptNumber));
    }

    // ── RetryJobAsync — returned JobRunResultDto ──────────────────────────────

    [Test]
    public async Task RetryJobAsync_WhenSimulatorReturnsSuccess_ReturnsDtoWithAttemptNumber2()
    {
        // Arrange — job already has one failed execution (attempt #1)
        await using var db = CreateDb();
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);

        db.JobExecutions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = job.Id,
            AttemptNumber    = 1,
            Status           = JobStatus.Failed,
            FailureType      = FailureType.Timeout,
            ExecutedAt       = DateTime.UtcNow.AddHours(-1),
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db, JobStatus.Success);

        // Act
        JobRunResultDto result = await sut.RetryJobAsync(job.Id);

        // Assert — retry #1: attempt #2, Success, RetryCount=1, 2 retries left
        Assert.Multiple(() =>
        {
            Assert.That(result.JobStatus,                Is.EqualTo("Success"));
            Assert.That(result.RetryCount,               Is.EqualTo(1));
            Assert.That(result.RetriesRemaining,         Is.EqualTo(JobService.MaxRetries - 1));
            Assert.That(result.Execution.AttemptNumber,  Is.EqualTo(2));
            Assert.That(result.Execution.Status,         Is.EqualTo("Success"));
            Assert.That(result.Execution.FailureType,    Is.Null);
        });
    }

    // ── RetryJobAsync — retry cap ─────────────────────────────────────────────

    /// <summary>
    /// Helper: seeds a job that has already consumed <paramref name="retryCount"/> retries,
    /// all of which failed. Returns the job with Status=Failed and RetryCount set.
    /// </summary>
    private static async Task<IntegrationJob> SeedExhaustedJobAsync(
        AppDbContext db, int retryCount)
    {
        var (_, job) = await SeedJobAsync(db, JobStatus.Failed);
        job.RetryCount = retryCount;

        // Add matching execution history (initial run + retryCount retries)
        for (int i = 1; i <= retryCount + 1; i++)
        {
            db.JobExecutions.Add(new JobExecution
            {
                Id               = Guid.NewGuid(),
                IntegrationJobId = job.Id,
                AttemptNumber    = i,
                Status           = JobStatus.Failed,
                FailureType      = FailureType.Timeout,
                ExecutedAt       = DateTime.UtcNow.AddHours(-retryCount + i - 1),
            });
        }
        await db.SaveChangesAsync();
        return job;
    }

    [Test]
    public async Task RetryJobAsync_FirstRetry_IncrementsRetryCountTo1()
    {
        // Arrange — job has 0 retries consumed so far
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: 0);
        var sut = BuildService(db, JobStatus.Failed, FailureType.NetworkError);

        // Act
        await sut.RetryJobAsync(job.Id);

        // Assert — RetryCount incremented to 1
        var updated = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updated!.RetryCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RetryJobAsync_ThirdRetryFails_JobBecomePermanentlyFailed()
    {
        // Arrange — job has already consumed 2 retries; this will be the 3rd (final)
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: 2);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act — 3rd retry, simulator returns Failed
        await sut.RetryJobAsync(job.Id);

        // Assert — job is now PermanentlyFailed and RetryCount = 3
        var updated = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Status,     Is.EqualTo(JobStatus.PermanentlyFailed));
            Assert.That(updated!.RetryCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task RetryJobAsync_ThirdRetryFails_StillCreatesJobExecutionRecord()
    {
        // Even when the final retry locks the job, a JobExecution row must be persisted
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: 2);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        var countBefore = await db.JobExecutions
            .CountAsync(e => e.IntegrationJobId == job.Id);

        // Act
        await sut.RetryJobAsync(job.Id);

        var countAfter = await db.JobExecutions
            .CountAsync(e => e.IntegrationJobId == job.Id);

        Assert.That(countAfter, Is.EqualTo(countBefore + 1));
    }

    [Test]
    public async Task RetryJobAsync_AfterRetryLimitExceeded_ThrowsWithManualInterventionMessage()
    {
        // Arrange — job already at MaxRetries (3); next call should be refused
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: JobService.MaxRetries);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RetryJobAsync(job.Id));

        Assert.That(ex!.Message, Does.Contain("manual intervention").IgnoreCase);
    }

    [Test]
    public async Task RetryJobAsync_AfterRetryLimitExceeded_JobStatusIsPermanentlyFailed()
    {
        // Arrange
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: JobService.MaxRetries);
        var sut = BuildService(db, JobStatus.Failed, FailureType.Timeout);

        // Act — swallow the expected exception
        Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryJobAsync(job.Id));

        // Assert
        var updated = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.That(updated!.Status, Is.EqualTo(JobStatus.PermanentlyFailed));
    }

    [Test]
    public async Task RetryJobAsync_ThirdRetrySucceeds_JobStatusIsSuccessNotPermanentlyFailed()
    {
        // Even on the 3rd retry, if the simulator returns Success the job should succeed
        await using var db = CreateDb();
        var job = await SeedExhaustedJobAsync(db, retryCount: 2);
        var sut = BuildService(db, JobStatus.Success);   // simulator returns Success

        // Act
        await sut.RetryJobAsync(job.Id);

        // Assert — job succeeded, NOT permanently failed
        var updated = await db.IntegrationJobs.FindAsync(job.Id);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Status,     Is.EqualTo(JobStatus.Success));
            Assert.That(updated!.RetryCount, Is.EqualTo(3));
        });
    }
}
