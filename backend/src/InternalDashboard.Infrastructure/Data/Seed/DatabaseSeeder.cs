using Bogus;
using InternalDashboard.Core.Models;
using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternalDashboard.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    private static readonly string[] JobNames =
    [
        "Sync CRM Contacts",
        "Export Invoices to ERP",
        "Pull Salesforce Opportunities",
        "Push Orders to Warehouse",
        "Fetch Exchange Rates",
        "Sync HR Employee Records",
        "Generate Monthly Report",
        "Archive Old Tickets",
        "Send Weekly Digest Email",
        "Reconcile Payment Ledger",
        "Ingest Stripe Webhooks",
        "Mirror JIRA Issues",
        "Update Product Catalogue",
        "Backup Customer Metadata",
        "Refresh Auth Tokens",
        "Import Vendor Price List",
        "Post Slack Notifications",
        "Sync Google Calendar Events",
        "Validate Tax Codes",
        "Upload Audit Logs to S3",
    ];

    // All non-null failure types available for random assignment
    private static readonly FailureType[] FailureTypes =
    [
        FailureType.Timeout,
        FailureType.AuthenticationError,
        FailureType.RateLimitExceeded,
        FailureType.NetworkError,
        FailureType.DataValidationError,
        FailureType.ThirdPartyServiceDown,
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Customers.AnyAsync())
        {
            logger.LogInformation("Seed: data already present — skipping.");
            return;
        }

        logger.LogInformation("Seed: starting database seeding...");

        var random = new Random();

        // ── 1. Customers (exactly 50) ─────────────────────────────────────────
        var customerFaker = new Faker<Customer>()
            .StrictMode(false)
            .RuleFor(c => c.Id,        _ => Guid.NewGuid())
            .RuleFor(c => c.Name,      f => f.Company.CompanyName())
            .RuleFor(c => c.Email,     (f, c) =>
            {
                var domain = c.Name
                    .ToLowerInvariant()
                    .Replace(",", "")
                    .Replace(".", "")
                    .Replace("'", "")
                    .Replace("&", "and")
                    .Replace(" ", "-")
                    .Trim('-');
                return $"admin@{domain}.com";
            })
            .RuleFor(c => c.CreatedAt, f => f.Date.PastOffset(3).UtcDateTime);

        var customers = customerFaker.Generate(70)
            .GroupBy(c => c.Email)
            .Select(g => g.First())
            .DistinctBy(c => c.Name)
            .Take(50)
            .ToList();

        foreach (var c in customers)
            c.Id = Guid.NewGuid();

        await db.Customers.AddRangeAsync(customers);
        await db.SaveChangesAsync();
        logger.LogInformation("Seed: inserted {Count} customers.", customers.Count);

        // ── 2. IntegrationJobs + JobExecutions ────────────────────────────────
        var allJobs       = new List<IntegrationJob>();
        var allExecutions = new List<JobExecution>();

        foreach (var customer in customers)
        {
            int jobCount = random.Next(3, 11); // 3–10 inclusive

            var pickedNames = JobNames
                .OrderBy(_ => random.Next())
                .Take(jobCount)
                .ToList();

            foreach (var jobName in pickedNames)
            {
                var jobId = Guid.NewGuid();

                var jobCreatedAt = customer.CreatedAt
                    .AddDays(random.Next(1, 61))
                    .AddHours(random.Next(0, 24))
                    .AddMinutes(random.Next(0, 60));

                // Build executions first — JobStatus is derived from them
                List<JobExecution> executions;

                if (jobCreatedAt > DateTime.UtcNow.AddDays(-2))
                {
                    // ── Pending: job was just created, hasn't run yet ─────────
                    executions = [];
                }
                else
                {
                    // Pick a behaviour pattern randomly
                    int pattern = random.Next(0, 4);
                    executions = pattern switch
                    {
                        // Pattern 0 — retry then succeed (most common)
                        0 => BuildRetryThenSucceed(jobId, jobCreatedAt, random),

                        // Pattern 1 — consistently failing
                        1 => BuildConsistentlyFailing(jobId, jobCreatedAt, random),

                        // Pattern 2 — stable (always succeeds first try)
                        2 => BuildStable(jobId, jobCreatedAt, random),

                        // Pattern 3 — flaky (mixed results across multiple runs)
                        _ => BuildFlaky(jobId, jobCreatedAt, random),
                    };
                }

                // ── Derive JobStatus from the last execution ──────────────────
                // No executions         → Pending
                // Last attempt Succeed  → Success
                // Last attempt Failed   → Failed
                JobStatus derivedStatus = executions.Count == 0
                    ? JobStatus.Pending
                    : executions.OrderBy(e => e.AttemptNumber).Last().Status;

                allJobs.Add(new IntegrationJob
                {
                    Id         = jobId,
                    CustomerId = customer.Id,
                    JobName    = jobName,
                    Status     = derivedStatus,
                    CreatedAt  = jobCreatedAt,
                });

                allExecutions.AddRange(executions);
            }
        }

        await db.IntegrationJobs.AddRangeAsync(allJobs);
        await db.SaveChangesAsync();
        logger.LogInformation("Seed: inserted {Count} integration jobs.", allJobs.Count);

        await db.JobExecutions.AddRangeAsync(allExecutions);
        await db.SaveChangesAsync();
        logger.LogInformation("Seed: inserted {Count} job executions.", allExecutions.Count);

        logger.LogInformation("Seed: completed successfully.");
    }

    // ── Execution pattern builders ────────────────────────────────────────────

    /// <summary>
    /// Attempt 1: Failed (random FailureType)
    /// Attempt 2: Success (FailureType = null)
    /// </summary>
    private static List<JobExecution> BuildRetryThenSucceed(
        Guid jobId, DateTime jobCreatedAt, Random random)
    {
        var executions  = new List<JobExecution>();
        var attemptTime = jobCreatedAt.AddMinutes(random.Next(5, 31));

        // Attempt 1 — fails
        executions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = jobId,
            AttemptNumber    = 1,
            Status           = JobStatus.Failed,
            FailureType      = FailureTypes[random.Next(FailureTypes.Length)],
            ExecutedAt       = attemptTime,
        });

        // Attempt 2 — succeeds (5–60 min later)
        attemptTime = attemptTime.AddMinutes(random.Next(5, 61));
        executions.Add(new JobExecution
        {
            Id               = Guid.NewGuid(),
            IntegrationJobId = jobId,
            AttemptNumber    = 2,
            Status           = JobStatus.Success,
            FailureType      = null,
            ExecutedAt       = attemptTime,
        });

        return executions;
    }

    /// <summary>
    /// Attempts 1–3: all Failed, each with AuthenticationError
    /// (can be extended to other failure types via the random param)
    /// </summary>
    private static List<JobExecution> BuildConsistentlyFailing(
        Guid jobId, DateTime jobCreatedAt, Random random)
    {
        var executions  = new List<JobExecution>();
        var attemptTime = jobCreatedAt.AddMinutes(random.Next(5, 31));

        // Pin one failure type for the whole job to show a systematic error
        var pinnedFailure = FailureTypes[random.Next(FailureTypes.Length)];
        int attempts      = random.Next(1, 4); // 1–3 attempts before giving up

        for (int i = 1; i <= attempts; i++)
        {
            executions.Add(new JobExecution
            {
                Id               = Guid.NewGuid(),
                IntegrationJobId = jobId,
                AttemptNumber    = i,
                Status           = JobStatus.Failed,
                FailureType      = pinnedFailure,
                ExecutedAt       = attemptTime,
            });

            attemptTime = attemptTime.AddMinutes(random.Next(5, 61));
        }

        return executions;
    }

    /// <summary>
    /// Attempts 1–5: all Success, FailureType = null (stable job)
    /// </summary>
    private static List<JobExecution> BuildStable(
        Guid jobId, DateTime jobCreatedAt, Random random)
    {
        var executions  = new List<JobExecution>();
        var attemptTime = jobCreatedAt.AddMinutes(random.Next(5, 31));
        int runs        = random.Next(1, 6); // 1–5 successful runs

        for (int i = 1; i <= runs; i++)
        {
            executions.Add(new JobExecution
            {
                Id               = Guid.NewGuid(),
                IntegrationJobId = jobId,
                AttemptNumber    = i,
                Status           = JobStatus.Success,
                FailureType      = null,
                ExecutedAt       = attemptTime,
            });

            attemptTime = attemptTime.AddMinutes(random.Next(5, 61));
        }

        return executions;
    }

    /// <summary>
    /// Mixed results: some failures with varying FailureTypes, some successes.
    /// The last attempt randomly succeeds or fails.
    /// </summary>
    private static List<JobExecution> BuildFlaky(
        Guid jobId, DateTime jobCreatedAt, Random random)
    {
        var executions  = new List<JobExecution>();
        var attemptTime = jobCreatedAt.AddMinutes(random.Next(5, 31));
        int runs        = random.Next(2, 6); // 2–5 runs

        for (int i = 1; i <= runs; i++)
        {
            bool isLast   = i == runs;
            bool succeeds = isLast
                ? random.Next(0, 2) == 0           // last attempt 50/50
                : random.Next(0, 3) == 0;           // earlier attempts ~33% success

            executions.Add(new JobExecution
            {
                Id               = Guid.NewGuid(),
                IntegrationJobId = jobId,
                AttemptNumber    = i,
                Status           = succeeds ? JobStatus.Success : JobStatus.Failed,
                FailureType      = succeeds
                    ? null
                    : FailureTypes[random.Next(FailureTypes.Length)],
                ExecutedAt       = attemptTime,
            });

            attemptTime = attemptTime.AddMinutes(random.Next(5, 61));
        }

        return executions;
    }
}