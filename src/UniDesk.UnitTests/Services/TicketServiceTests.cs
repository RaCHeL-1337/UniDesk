using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Options;
using UniDesk.Web.Services;

namespace UniDesk.UnitTests.Services
{
    public class TicketServiceTests
    {
        [Fact]
        public void UpdateStatus_ShouldChangeStatus_WhenValid()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var created = service.Create(new CreateTicketRequest
            {
                Title = "Test",
                Description = "Test"
            });

            service.UpdateStatus(created.Id, TicketStatus.Closed);

            var ticket = service.GetById(created.Id);
            Assert.Equal(TicketStatus.Closed, ticket.Status);
        }

        [Fact]
        public void UpdateStatus_ShouldThrowException_WhenTicketIsAlreadyClosed()
        {
            using var db = CreateDbContext();
            var service = CreateService(db);

            var created = service.Create(new CreateTicketRequest
            {
                Title = "Test",
                Description = "Test"
            });

            service.UpdateStatus(created.Id, TicketStatus.Closed);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                service.UpdateStatus(created.Id, TicketStatus.InProgress));

            Assert.Equal("Ticket is already closed.", exception.Message);
        }

        private static UniDeskDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<UniDeskDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new UniDeskDbContext(options);
        }

        private static TicketService CreateService(UniDeskDbContext db)
        {
            return new TicketService(
                db,
                NullLogger<TicketService>.Instance,
                Options.Create(new DiagnosticsOptions()),
                new SafeMarkdownRenderer());
        }
    }
}
