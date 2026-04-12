using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public class TicketService
    {
        public void UpdateStatus(Ticket ticket, TicketStatus status)
        {
            if (ticket.Status == TicketStatus.Closed)
            {
                throw new InvalidOperationException("Ticket is already closed.");
            }

            ticket.Status = status;
        }
    }
}