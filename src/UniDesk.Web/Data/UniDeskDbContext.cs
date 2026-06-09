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
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Ticket>(entity =>
            {
                entity.Property(ticket => ticket.CreatedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(ticket => ticket.CreatedByEmail)
                    .HasMaxLength(256)
                    .IsRequired();
            });

            builder.Entity<TicketComment>(entity =>
            {
                entity.HasOne(comment => comment.Ticket)
                    .WithMany(ticket => ticket.Comments)
                    .HasForeignKey(comment => comment.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges()
        {
            ApplyAuditValues();

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditValues();

            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditValues()
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

            var commentEntries = ChangeTracker.Entries<TicketComment>();

            foreach (var entry in commentEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
