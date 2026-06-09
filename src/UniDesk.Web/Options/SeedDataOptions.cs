namespace UniDesk.Web.Options;

public class SeedDataOptions
{
    public string AdminEmail { get; set; } = "admin@unidesk.local";
    public string AdminPassword { get; set; } = "Admin123!";
    public string AdminOrganizationName { get; set; } = "UniDesk Lab";
    public List<SeedTicketOptions> Tickets { get; set; } = new();
}

public class SeedTicketOptions
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public List<string> Comments { get; set; } = new();
}
