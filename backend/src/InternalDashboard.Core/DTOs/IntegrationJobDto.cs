namespace InternalDashboard.Core.DTOs;

/// <summary>Read-only projection of an IntegrationJob (list view).</summary>
public sealed record IntegrationJobDto(
    Guid     Id,
    Guid     CustomerId,
    string   CustomerName,
    string   JobName,
    string   Status,
    DateTime CreatedAt,
    int      TotalExecutions,
    DateTime? LastExecutedAt,
    int      RetryCount
);

/// <summary>Detail view — includes execution history.</summary>
public sealed record IntegrationJobDetailDto(
    Guid     Id,
    Guid     CustomerId,
    string   CustomerName,
    string   JobName,
    string   Status,
    DateTime CreatedAt,
    IReadOnlyList<JobExecutionDto> Executions
);

/// <summary>
/// Flat join of the 5 most recent job executions with their parent job + customer info.
/// Used by the "Recent Job Executions" table on the dashboard.
/// </summary>
public sealed record RecentJobExecutionDto(
    string   JobName,
    string   CustomerName,
    string   Status,
    string?  FailureType,
    DateTime ExecutedAt
);
