using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalDashboard.API.Controllers;

/// <summary>
/// Thin controller for integration job lifecycle operations.
/// Contains zero business logic — every call is delegated directly to IJobService.
/// Error handling is centralised in GlobalExceptionMiddleware.
///
/// Routes:
///   POST /api/jobs/{id}/run    →  IJobService.RunJobAsync
///   POST /api/jobs/{id}/retry  →  IJobService.RetryJobAsync
/// </summary>
[ApiController]
[Route("api/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// Triggers a fresh run of the specified integration job.
    /// </summary>
    /// <param name="id">The ID of the IntegrationJob to run.</param>
    /// <response code="200">Returns the execution record and updated job state.</response>
    /// <response code="404">Job not found.</response>
    /// <response code="400">Job is not in a runnable state.</response>
    [HttpPost("{id:guid}/run")]
    [ProducesResponseType(typeof(JobRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobRunResultDto>> RunJob([FromRoute] Guid id)
    {
        var result = await _jobService.RunJobAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Retries a previously failed integration job.
    /// Only jobs with Status = Failed and remaining retries are accepted.
    /// After 3 failed retries the job is marked PermanentlyFailed and requires manual intervention.
    /// </summary>
    /// <param name="id">The ID of the failed IntegrationJob to retry.</param>
    /// <response code="200">Returns the execution record, updated job status and retries remaining.</response>
    /// <response code="404">Job not found.</response>
    /// <response code="400">Job is not retryable (wrong status or retry limit reached).</response>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(JobRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobRunResultDto>> RetryJob([FromRoute] Guid id)
    {
        var result = await _jobService.RetryJobAsync(id);
        return Ok(result);
    }
}
