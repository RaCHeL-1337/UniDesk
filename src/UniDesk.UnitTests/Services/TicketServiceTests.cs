using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using UniDesk.Web.Models;
using UniDesk.Web.Services;
using UniDesk.UnitTests.Fakes;

namespace UniDesk.UnitTests.Services
{
    public class TicketServiceTests
    {
        [Fact]
        public void UpdateStatus_ShouldChangeStatus_WhenValid()
        {
            var service = new TicketService();

            var ticket = new Ticket()
            {
                Title = "Test",
                Description = "Test",
                Status = TicketStatus.New
            };

            service.UpdateStatus(ticket, TicketStatus.Closed);
            Assert.Equal(TicketStatus.Closed, ticket.Status);
        }

        [Fact]
        public void UpdateStatus_ShouldThrowException_WhenTicketIsAlreadyClosed()
        {
            var service = new TicketService();

            var ticket = new Ticket()
            {
                Title = "Test",
                Description = "Test",
                Status = TicketStatus.Closed
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                service.UpdateStatus(ticket, TicketStatus.InProgress));

            Assert.Equal("Ticket is already closed.", exception.Message);
        }
    }
}
