using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace UniDesk.Web.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? OrganizationName { get; set; }
}
