using InternalDashboard.Core.Models.Enums;

namespace InternalDashboard.Core.Models;

public class IntegrationJob
{
    public Guid      Id         { get; set; }
    public Guid      CustomerId { get; set; }
    public string    JobName    { get; set; } = string.Empty;
    public JobStatus Status     { get; set; }
    public DateTime  CreatedAt  { get; set; }

    /// <summary>
    /// Number of retry attempts made after the initial run.
    /// Incremented each time RetryJobAsync is called.
    /// Once this reaches <see cref="JobService.MaxRetries"/> (3), the job becomes
    /// PermanentlyFailed and can no longer be retried automatically.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    // Navigation
    public Customer                  Customer       { get; set; } = null!;
    public ICollection<JobExecution> JobExecutions  { get; set; } = [];
}
