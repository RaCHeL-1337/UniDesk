using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public class InMemoryTicketService : ITicketService
    {
        private static readonly List<Ticket> _tickets = new();

        public IEnumerable<Ticket> GetAll()
        {
            return _tickets;
        }

        public void Add(Ticket ticket)
        {
            ticket.Id = _tickets.Count == 0 ? 1 : _tickets.Max(t => t.Id) + 1;
            ticket.CreatedAt = DateTime.Now;
            ticket.UpdatedAt = DateTime.Now;
            ticket.Status = TicketStatus.New;

            _tickets.Add(ticket);
        }

        public Ticket? GetById(int id)
        {
            return _tickets.FirstOrDefault(t => t.Id == id);
        }

        public void Update(Ticket ticket)
        {
            var existing = _tickets.FirstOrDefault(t => t.Id == ticket.Id);

            if (existing != null)
            {
                existing.Title = ticket.Title;
                existing.Description = ticket.Description;
                existing.Status = ticket.Status;
                existing.UpdatedAt = DateTime.Now;
            }
        }

        public IEnumerable<Ticket> Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _tickets;

            return _tickets.Where(t =>
                t.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        }
    }
}