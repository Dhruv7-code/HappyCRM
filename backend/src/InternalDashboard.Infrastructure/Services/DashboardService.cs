using InternalDashboard.Core.DTOs;
using InternalDashboard.Core.Models.Enums;
using InternalDashboard.Core.Services;
using InternalDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InternalDashboard.Infrastructure.Services;

/// <summary>
/// Read-only query service — projects EF entities into DTOs for the dashboard UI.
/// All queries are no-tracking for maximum read performance.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    // ── Customers ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync()
        => await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto(
                c.Id,
                c.Name,
                c.Email,
                c.CreatedAt,
                c.IntegrationJobs.Count))
            .ToListAsync();

    /// <summary>
    /// Joins Customers → IntegrationJobs and counts only active jobs per customer
    /// (Pending = 0 or Running = 1). EF Core opens and closes the connection automatically.
    /// </summary>
    public async Task<IReadOnlyList<CustomerListDto>> GetCustomerListAsync()
    {
        try
        {
            return await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CustomerListDto(
                    c.Id,
                    c.Name,
                    c.Email,
                    c.CreatedAt,
                    c.IntegrationJobs.Count(j =>
                        j.Status == JobStatus.Pending ||
                        j.Status == JobStatus.Running)))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve customer list from the database.", ex);
        }
    }

    // ── Jobs ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<IntegrationJobDto>> GetJobsAsync(Guid? customerId = null)
    {
        var q = _db.IntegrationJobs
            .AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.JobExecutions)
            .AsQueryable();

        if (customerId.HasValue)
            q = q.Where(j => j.CustomerId == customerId.Value);

        var jobs = await q
            .Select(j => new IntegrationJobDto(
                j.Id,
                j.CustomerId,
                j.Customer.Name,
                j.JobName,
                j.Status.ToString(),
                j.CreatedAt,
                j.JobExecutions.Count,
                j.JobExecutions
                    .OrderByDescending(e => e.ExecutedAt)
                    .Select(e => (DateTime?)e.ExecutedAt)
                    .FirstOrDefault(),
                j.RetryCount))
            .ToListAsync();

        // Sort: jobs with executions first (lastExecutedAt DESC), then never-run jobs (createdAt DESC)
        return jobs
            .OrderByDescending(j => j.LastExecutedAt.HasValue ? 1 : 0)
            .ThenByDescending(j => j.LastExecutedAt)
            .ThenByDescending(j => j.CreatedAt)
            .ToList();
    }

    // ── Job detail ────────────────────────────────────────────────────────────

    public async Task<IntegrationJobDetailDto?> GetJobDetailAsync(Guid jobId)
    {
        var job = await _db.IntegrationJobs
            .AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.JobExecutions)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job is null) return null;

        var executions = job.JobExecutions
            .OrderByDescending(e => e.ExecutedAt)
            .Select(e => new JobExecutionDto(
                e.Id,
                e.AttemptNumber,
                e.Status.ToString(),
                e.FailureType?.ToString(),
                e.ExecutedAt))
            .ToList();

        return new IntegrationJobDetailDto(
            job.Id,
            job.CustomerId,
            job.Customer.Name,
            job.JobName,
            job.Status.ToString(),
            job.CreatedAt,
            executions);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var customers  = await _db.Customers.AsNoTracking().CountAsync();
        var jobs       = await _db.IntegrationJobs.AsNoTracking().CountAsync();
        var executions = await _db.JobExecutions.AsNoTracking().CountAsync();

        var statusCounts = await _db.IntegrationJobs
            .AsNoTracking()
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int success = statusCounts.FirstOrDefault(s => s.Status.ToString() == "Success")?.Count ?? 0;
        int failed  = statusCounts.FirstOrDefault(s => s.Status.ToString() == "Failed")?.Count  ?? 0;
        int running = statusCounts.FirstOrDefault(s => s.Status.ToString() == "Running")?.Count ?? 0;
        int pending = statusCounts.FirstOrDefault(s => s.Status.ToString() == "Pending")?.Count ?? 0;

        double rate = jobs > 0 ? Math.Round((double)success / jobs * 100, 1) : 0;

        return new DashboardStatsDto(customers, jobs, executions, success, failed, running, pending, rate);
    }

    // ── Recent executions ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RecentJobExecutionDto>> GetRecentJobExecutionsAsync()
        => await _db.JobExecutions
            .AsNoTracking()
            .OrderByDescending(e => e.ExecutedAt)
            .Take(5)
            .Select(e => new RecentJobExecutionDto(
                e.IntegrationJob.JobName,
                e.IntegrationJob.Customer.Name,
                e.Status.ToString(),
                e.FailureType == null ? null : e.FailureType.ToString(),
                e.ExecutedAt))
            .ToListAsync();

    public async Task<IReadOnlyList<ExecutionListDto>> GetAllExecutionsAsync()
        => await _db.JobExecutions
            .AsNoTracking()
            .OrderByDescending(e => e.ExecutedAt)
            .Select(e => new ExecutionListDto(
                e.Id,
                e.IntegrationJobId,
                e.IntegrationJob.JobName,
                e.IntegrationJob.Customer.Name,
                e.Status.ToString(),
                e.FailureType == null ? null : e.FailureType.ToString(),
                e.AttemptNumber,
                e.ExecutedAt))
            .ToListAsync();

    // ── Create customer ───────────────────────────────────────────────────────

    public async Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Raise if email already exists
        bool emailTaken = await _db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Email.ToLower() == request.Email.ToLower());

        if (emailTaken)
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists.");

        var customer = new InternalDashboard.Core.Models.Customer
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name.Trim(),
            Email     = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return new CreateCustomerResponse(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.CreatedAt);
    }

    // ── Update customer ───────────────────────────────────────────────────────

    public async Task<UpdateCustomerResponse> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request)
    {
        // Fetch the tracked entity — no AsNoTracking so EF can save changes
        var customer = await _db.Customers.FindAsync(customerId)
            ?? throw new KeyNotFoundException($"Customer '{customerId}' not found.");

        // If the email is changing, make sure it isn't already taken by someone else
        bool emailTaken = await _db.Customers
            .AnyAsync(c => c.Email.ToLower() == request.Email.ToLower() && c.Id != customerId);

        if (emailTaken)
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists.");

        customer.Name  = request.Name.Trim();
        customer.Email = request.Email.Trim();

        await _db.SaveChangesAsync();

        return new UpdateCustomerResponse(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.CreatedAt);
    }
}
