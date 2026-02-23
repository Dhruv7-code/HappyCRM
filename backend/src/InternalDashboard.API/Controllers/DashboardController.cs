using InternalDashboard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalDashboard.API.Controllers;

/// <summary>
/// Read-only dashboard data endpoints consumed by the frontend.
///
/// Routes:
///   GET /api/dashboard/stats        :  headline KPI cards
///   GET /api/dashboard/customers    :  customer list
///   GET /api/dashboard/jobs         :  jobs list (optional ?customerId=)
///   GET /api/dashboard/jobs/{id}    :  job detail + execution history
///   GET /api/dashboard/recent-executions →: recent job executions
/// </summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Returns headline stats for the overview KPI cards.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await _dashboard.GetStatsAsync());

    /// <summary>Returns all customers with their job count.</summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
        => Ok(await _dashboard.GetCustomersAsync());

    /// <summary>
    /// Returns all integration jobs (optionally filtered by customer).
    /// </summary>
    /// <param name="customerId">Optional: filter to a specific customer's jobs.</param>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] Guid? customerId = null)
        => Ok(await _dashboard.GetJobsAsync(customerId));

    /// <summary>Returns a single job with its full execution history.</summary>
    /// <param name="id">The job ID.</param>
    /// <response code="200">Job found and returned.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("jobs/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobDetail([FromRoute] Guid id)
    {
        var detail = await _dashboard.GetJobDetailAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Returns the 5 most recent job executions (flat join of execution + job + customer).</summary>
    [HttpGet("recent-executions")]
    public async Task<IActionResult> GetRecentExecutions()
        => Ok(await _dashboard.GetRecentJobExecutionsAsync());

    /// <summary>Returns all job executions ordered by ExecutedAt descending.</summary>
    [HttpGet("executions")]
    public async Task<IActionResult> GetAllExecutions()
        => Ok(await _dashboard.GetAllExecutionsAsync());
}
