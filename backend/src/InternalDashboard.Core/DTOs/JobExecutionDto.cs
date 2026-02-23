namespace InternalDashboard.Core.DTOs;

/// <summary>Read-only projection of a JobExecution.</summary>
public sealed record JobExecutionDto(
    Guid     Id,
    int      AttemptNumber,
    string   Status,
    string?  FailureType,
    DateTime ExecutedAt
);

/// <summary>
/// Flat projection of a JobExecution joined with its parent job + customer.
/// Used by the Executions page table.
/// </summary>
public sealed record ExecutionListDto(
    Guid     ExecutionId,
    Guid     JobId,
    string   JobName,
    string   CustomerName,
    string   Status,
    string?  FailureType,
    int      AttemptNumber,
    DateTime ExecutedAt
);
