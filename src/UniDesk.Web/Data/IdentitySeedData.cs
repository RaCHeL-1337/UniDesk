using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniDesk.Web.Models;
using UniDesk.Web.Options;

namespace UniDesk.Web.Data;

public static class IdentitySeedData
{
    public static async Task EnsureSeedUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

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

        var email = seedOptions.AdminEmail;
        var password = seedOptions.AdminPassword;
        var organizationName = seedOptions.AdminOrganizationName;

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

        if (!db.Tickets.Any() && seedOptions.Tickets.Count > 0)
        {
            foreach (var seedTicket in seedOptions.Tickets)
            {
                var ticket = new Ticket
                {
                    Title = seedTicket.Title,
                    Description = seedTicket.Description,
                    CreatedByUserId = admin.Id,
                    CreatedByEmail = admin.Email ?? email,
                    Status = Enum.TryParse<TicketStatus>(seedTicket.Status, out var status)
                        ? status
                        : TicketStatus.New
                };

                foreach (var message in seedTicket.Comments.Where(comment => !string.IsNullOrWhiteSpace(comment)))
                {
                    ticket.Comments.Add(new TicketComment
                    {
                        AuthorId = admin.Id,
                        AuthorEmail = admin.Email ?? email,
                        Message = message
                    });
                }

                db.Tickets.Add(ticket);
            }

            db.SaveChanges();
        }
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(e => e.Description));
    }
}
