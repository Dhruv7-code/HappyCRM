namespace InternalDashboard.Core.Models.Enums;

public enum JobStatus
{
    Pending          = 0,
    Running          = 1,
    Success          = 2,
    Failed           = 3,
    /// <summary>
    /// Set when a job has failed and exhausted all 3 retry attempts.
    /// The job is locked — manual intervention is required to reset it.
    /// </summary>
    PermanentlyFailed = 4
}
