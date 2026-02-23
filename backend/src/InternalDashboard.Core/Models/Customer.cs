namespace InternalDashboard.Core.Models;

public class Customer
{
    public Guid   Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<IntegrationJob> IntegrationJobs { get; set; } = [];
}
