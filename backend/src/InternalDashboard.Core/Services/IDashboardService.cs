using InternalDashboard.Core.DTOs;

namespace InternalDashboard.Core.Services;

/// <summary>
/// Read-only query service for the dashboard UI.
/// Returns projected DTOs — never raw EF entities.
/// </summary>
public interface IDashboardService
{
    /// <summary>Returns all customers with their total job count.</summary>
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync();

    /// <summary>
    /// Returns all customers joined with their active job count (Pending or Running).
    /// Used by the Customers page table.
    /// </summary>
    Task<IReadOnlyList<CustomerListDto>> GetCustomerListAsync();

    /// <summary>
    /// Returns all integration jobs with a summary row per job.
    /// Optionally filtered by customerId.
    /// </summary>
    Task<IReadOnlyList<IntegrationJobDto>> GetJobsAsync(Guid? customerId = null);

    /// <summary>Returns a single job with its full execution history, or null if not found.</summary>
    Task<IntegrationJobDetailDto?> GetJobDetailAsync(Guid jobId);

    /// <summary>Returns headline stats for the overview cards.</summary>
    Task<DashboardStatsDto> GetStatsAsync();

    /// <summary>
    /// Returns the 5 most recent job executions (flat join of execution + job + customer).
    /// Ordered by ExecutedAt descending.
    /// </summary>
    Task<IReadOnlyList<RecentJobExecutionDto>> GetRecentJobExecutionsAsync();

    /// <summary>
    /// Returns all job executions as a flat join with job + customer data.
    /// Ordered by ExecutedAt descending.
    /// </summary>
    Task<IReadOnlyList<ExecutionListDto>> GetAllExecutionsAsync();

    /// <summary>
    /// Creates a new customer with a generated UUID and the current UTC timestamp.
    /// Throws <see cref="InvalidOperationException"/> if the email is already in use.
    /// </summary>
    Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);

    /// <summary>
    /// Updates the name and/or email of an existing customer.
    /// Throws <see cref="KeyNotFoundException"/> if the customer does not exist.
    /// Throws <see cref="InvalidOperationException"/> if the new email is already taken by another customer.
    /// </summary>
    Task<UpdateCustomerResponse> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request);
}
