using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.UnitTests.Services
{
    public class TicketServiceTests
    {
        [Fact]
        public void UpdateStatus_ShouldChangeStatus_WhenValid()
        {
            using var connection = CreateConnection();
            var service = CreateService(connection);

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
            using var connection = CreateConnection();
            var service = CreateService(connection);

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

        private static SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        }

        private static TicketService CreateService(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<UniDeskDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new UniDeskDbContext(options);
            db.Database.EnsureCreated();

            return new TicketService(db, NullLogger<TicketService>.Instance);
        }
    }
}
