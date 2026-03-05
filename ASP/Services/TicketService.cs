using ASP.Models;

namespace ASP.Services
{
    public class TicketService : ITicketService
    {
        private readonly List<Ticket> _tickets = new();

        public IEnumerable<Ticket> GetAllTickets()
        {
            return _tickets;
        }

        public void AddTicket(Ticket ticket)
        {
            ticket.CreatedAt = DateTime.Now;
            ticket.Status = TicketStatus.Open;

            _tickets.Add(ticket);
        }

        public Ticket? GetTicketById(int id)
        {
            return _tickets.FirstOrDefault(t => t.Id == id);
        }

        public IEnumerable<Ticket> SearchTickets(string search)
        {
            return _tickets.Where(t =>
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
    }
}