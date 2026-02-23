using InternalDashboard.Core.DTOs;

namespace InternalDashboard.Core.Services;

/// <summary>
/// Dedicated stats service — each method targets one headline KPI.
/// Consumed by StatsController to serve the frontend dashboard cards.
/// </summary>
public interface IStatsService
{
    /// <summary>Total number of customers in the database.</summary>
    Task<int> GetTotalCustomersAsync();

    /// <summary>Total number of jobs with Status = Failed or PermanentlyFailed.</summary>
    Task<int> GetTotalFailedJobsAsync();

    /// <summary>Total number of jobs with Status = Success.</summary>
    Task<int> GetTotalSuccessJobsAsync();

    /// <summary>Total number of jobs with Status = Pending.</summary>
    Task<int> GetTotalPendingJobsAsync();

    /// <summary>Success jobs as a percentage of all jobs (0–100, 1 decimal place).</summary>
    Task<double> GetSuccessPercentageAsync();

    /// <summary>Failed jobs (incl. PermanentlyFailed) as a percentage of all jobs.</summary>
    Task<double> GetFailedPercentageAsync();

    /// <summary>Pending jobs as a percentage of all jobs.</summary>
    Task<double> GetPendingPercentageAsync();

    /// <summary>All stats rolled up into a single DTO.</summary>
    Task<DashboardStatsDto> GetAllStatsAsync();
}
