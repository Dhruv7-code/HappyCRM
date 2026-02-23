using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalDashboard.API.Controllers;

/// <summary>
/// Customer resource endpoints.
///
/// Routes:
///   GET  /api/customers                    :  all customers with active job count
///   POST /api/customers/newcustomer        :  create a new customer
///   PUT  /api/customers/{id}/updateuser    :  update a customer's name and/or email
/// </summary>
[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public CustomersController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>
    /// Returns all customers joined with their active job count (Pending or Running).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var customers = await _dashboard.GetCustomerListAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new customer.
    /// Assigns a new UUID as the ID and records the current UTC time as CreatedAt.
    /// Returns 409 Conflict if the email is already in use.
    /// </summary>
    [HttpPost("newcustomer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Name and Email are required." });

        try
        {
            var result = await _dashboard.CreateCustomerAsync(request);
            return CreatedAtAction(nameof(GetAll), new { }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a customer's name and/or email.
    /// Returns 404 if the customer is not found.
    /// Returns 409 if the new email is already taken by another customer.
    /// </summary>
    [HttpPut("{id:guid}/updateuser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCustomer(
        [FromRoute] Guid id,
        [FromBody]  UpdateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Name and Email are required." });

        try
        {
            var result = await _dashboard.UpdateCustomerAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
