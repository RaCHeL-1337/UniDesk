using Microsoft.AspNetCore.Identity;
using UniDesk.Web.Models;

namespace UniDesk.Web.Data;

public static class IdentitySeedData
{
    public static async Task EnsureSeedUserAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not seed role {role}: {FormatErrors(roleResult)}");
            }
        }

        var email = configuration["Identity:AdminEmail"] ?? configuration["Identity:SeedEmail"] ?? "admin@unidesk.local";
        var password = configuration["Identity:AdminPassword"] ?? configuration["Identity:SeedPassword"] ?? "Admin123!";
        var organizationName = configuration["Identity:AdminOrganizationName"] ?? "UniDesk Lab";

        var admin = await userManager.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                OrganizationName = organizationName
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not seed default Admin user: {FormatErrors(createResult)}");
            }
        }
        else if (string.IsNullOrWhiteSpace(admin.OrganizationName))
        {
            admin.OrganizationName = organizationName;

            var updateResult = await userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not update default Admin user: {FormatErrors(updateResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign Admin role to default user: {FormatErrors(addToRoleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(e => e.Description));
    }
}
