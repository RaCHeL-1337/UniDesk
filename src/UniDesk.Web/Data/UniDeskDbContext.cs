using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Models;

namespace UniDesk.Web.Data
{
    public class UniDeskDbContext : IdentityDbContext<ApplicationUser>
    {
        public UniDeskDbContext(DbContextOptions<UniDeskDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets => Set<Ticket>();

        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries<Ticket>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChanges();
        }
    }
}
