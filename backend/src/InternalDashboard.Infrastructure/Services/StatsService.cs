using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Core.Services;
using InternalDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InternalDashboard.Infrastructure.Services;

/// <summary>
/// Connects to the database and calculates per-status job counts and percentages.
/// All queries are no-tracking for read performance.
/// </summary>
public sealed class StatsService : IStatsService
{
    private readonly AppDbContext _db;

    public StatsService(AppDbContext db) => _db = db;

    // ── Customers ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<int> GetTotalCustomersAsync()
        => _db.Customers.AsNoTracking().CountAsync();

    // ── Job counts by status ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<int> GetTotalSuccessJobsAsync()
        => _db.IntegrationJobs
            .AsNoTracking()
            .CountAsync(j => j.Status == JobStatus.Success);

    /// <inheritdoc/>
    /// Counts both Failed and PermanentlyFailed as "failed".
    public Task<int> GetTotalFailedJobsAsync()
        => _db.IntegrationJobs
            .AsNoTracking()
            .CountAsync(j => j.Status == JobStatus.Failed
                          || j.Status == JobStatus.PermanentlyFailed);

    /// <inheritdoc/>
    public Task<int> GetTotalPendingJobsAsync()
        => _db.IntegrationJobs
            .AsNoTracking()
            .CountAsync(j => j.Status == JobStatus.Pending);

    // ── Percentages ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<double> GetSuccessPercentageAsync()
    {
        int total   = await _db.IntegrationJobs.AsNoTracking().CountAsync();
        int success = await GetTotalSuccessJobsAsync();
        return total > 0 ? Math.Round((double)success / total * 100, 1) : 0;
    }

    /// <inheritdoc/>
    public async Task<double> GetFailedPercentageAsync()
    {
        int total  = await _db.IntegrationJobs.AsNoTracking().CountAsync();
        int failed = await GetTotalFailedJobsAsync();
        return total > 0 ? Math.Round((double)failed / total * 100, 1) : 0;
    }

    /// <inheritdoc/>
    public async Task<double> GetPendingPercentageAsync()
    {
        int total   = await _db.IntegrationJobs.AsNoTracking().CountAsync();
        int pending = await GetTotalPendingJobsAsync();
        return total > 0 ? Math.Round((double)pending / total * 100, 1) : 0;
    }

    // ── Rolled-up DTO ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DashboardStatsDto> GetAllStatsAsync()
    {
        int customers  = await GetTotalCustomersAsync();
        int totalJobs  = await _db.IntegrationJobs.AsNoTracking().CountAsync();
        int executions = await _db.JobExecutions.AsNoTracking().CountAsync();
        int success    = await GetTotalSuccessJobsAsync();
        int failed     = await GetTotalFailedJobsAsync();
        int pending    = await GetTotalPendingJobsAsync();
        int running    = await _db.IntegrationJobs
                            .AsNoTracking()
                            .CountAsync(j => j.Status == JobStatus.Running);

        double successRate = totalJobs > 0
            ? Math.Round((double)success / totalJobs * 100, 1)
            : 0;

        return new DashboardStatsDto(
            customers,
            totalJobs,
            executions,
            success,
            failed,
            running,
            pending,
            successRate);
    }
}
