using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using UniDesk.Web.Models;

namespace UniDesk.UnitTests.Models
{
    public class TicketTests
    {
        [Fact]
        public void Ticket_ShouldHaveStatusNew_WhenCreated()
        {
            var ticket = new Ticket()
            {
                Title = "Test",
                Description = "Test"
            };

            var status = ticket.Status;
            var createdDate = ticket.CreatedAt;

            Assert.Equal(TicketStatus.New, status);
            Assert.NotEqual(default(DateTime), createdDate);
        }
    }
}
