using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.UnitTests.Fakes
{
    public class FakeTicketRepository
    {
        public Ticket TicketInMemory { get; set; }

        public bool WasUpdateCalled { get; private set; }

        public Ticket GetById(int id)
        {
            return TicketInMemory;
        }

        public void Update(Ticket ticket)
        {
            TicketInMemory = ticket;
            WasUpdateCalled = true;
        }
    }
}