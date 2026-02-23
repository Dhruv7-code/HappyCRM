namespace InternalDashboard.Core.DTOs;

/// <summary>Headline numbers shown on the dashboard overview cards.</summary>
public sealed record DashboardStatsDto(
    int   TotalCustomers,
    int   TotalJobs,
    int   TotalExecutions,
    int   SuccessCount,
    int   FailedCount,
    int   RunningCount,
    int   PendingCount,
    double SuccessRate        // 0–100 percentage
);
