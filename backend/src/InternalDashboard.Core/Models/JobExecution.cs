using InternalDashboard.Core.Models.Enums;

namespace InternalDashboard.Core.Models;

public class JobExecution
{
    public Guid         Id               { get; set; }
    public Guid         IntegrationJobId { get; set; }
    public int          AttemptNumber    { get; set; }
    public JobStatus    Status           { get; set; }
    public FailureType? FailureType      { get; set; }
    public DateTime     ExecutedAt       { get; set; }

    // Navigation
    public IntegrationJob IntegrationJob { get; set; } = null!;
}
