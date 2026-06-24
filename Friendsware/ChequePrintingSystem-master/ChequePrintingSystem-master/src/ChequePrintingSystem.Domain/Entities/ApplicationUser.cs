using Microsoft.AspNetCore.Identity;

namespace ChequePrintingSystem.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
