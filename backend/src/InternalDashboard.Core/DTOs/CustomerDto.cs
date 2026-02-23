namespace InternalDashboard.Core.DTOs;

/// <summary>Request body for creating a new customer.</summary>
public sealed record CreateCustomerRequest(string Name, string Email);

/// <summary>Response returned after a customer is successfully created.</summary>
public sealed record CreateCustomerResponse(Guid Id, string Name, string Email, DateTime CreatedAt);

/// <summary>Request body for updating an existing customer's name and/or email.</summary>
public sealed record UpdateCustomerRequest(string Name, string Email);

/// <summary>Response returned after a customer is successfully updated.</summary>
public sealed record UpdateCustomerResponse(Guid Id, string Name, string Email, DateTime CreatedAt);

/// <summary>Read-only projection of a Customer (summary — used by dashboard stats).</summary>
public sealed record CustomerDto(
    Guid     Id,
    string   Name,
    string   Email,
    DateTime CreatedAt,
    int      TotalJobs
);

/// <summary>
/// Customer list view — joins IntegrationJobs to count only active jobs
/// (Pending or Running). Used by the Customers page table.
/// </summary>
public sealed record CustomerListDto(
    Guid     Id,
    string   Name,
    string   Email,
    DateTime CreatedAt,
    int      ActiveJobs
);
