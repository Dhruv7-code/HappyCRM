using InternalDashboard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalDashboard.API.Controllers;

/// <summary>
/// Exposes individual stat endpoints and a rolled-up summary.
///
/// Routes:
///   GET /api/stats                  →  all stats as one DTO
///   GET /api/stats/customers/total  →  total customer count
///   GET /api/stats/jobs/success     →  success count + %
///   GET /api/stats/jobs/failed      →  failed count + %
///   GET /api/stats/jobs/pending     →  pending count + %
/// </summary>
[ApiController]
[Route("api/stats")]
public sealed class StatsController : ControllerBase
{
    private readonly IStatsService _stats;

    public StatsController(IStatsService stats) => _stats = stats;

    /// <summary>All headline stats in one call.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _stats.GetAllStatsAsync());

    /// <summary>Total number of customers.</summary>
    [HttpGet("customers/total")]
    public async Task<IActionResult> GetTotalCustomers()
        => Ok(new { total = await _stats.GetTotalCustomersAsync() });

    /// <summary>Total success job count and percentage.</summary>
    [HttpGet("jobs/success")]
    public async Task<IActionResult> GetSuccess()
        => Ok(new
        {
            count      = await _stats.GetTotalSuccessJobsAsync(),
            percentage = await _stats.GetSuccessPercentageAsync()
        });

    /// <summary>Total failed job count and percentage.</summary>
    [HttpGet("jobs/failed")]
    public async Task<IActionResult> GetFailed()
        => Ok(new
        {
            count      = await _stats.GetTotalFailedJobsAsync(),
            percentage = await _stats.GetFailedPercentageAsync()
        });

    /// <summary>Total pending job count and percentage.</summary>
    [HttpGet("jobs/pending")]
    public async Task<IActionResult> GetPending()
        => Ok(new
        {
            count      = await _stats.GetTotalPendingJobsAsync(),
            percentage = await _stats.GetPendingPercentageAsync()
        });
}
