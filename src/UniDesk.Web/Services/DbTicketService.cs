using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Data;
using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public class DbTicketService : ITicketService
    {
        private readonly UniDeskDbContext _context;

        public DbTicketService(UniDeskDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Ticket> GetAll()
        {
            return _context.Tickets.ToList();
        }

        public Ticket? GetById(int id)
        {
            return _context.Tickets.FirstOrDefault(t => t.Id == id);
        }

        public void Add(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            _context.SaveChanges();
        }

        public void Update(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }
    }
}