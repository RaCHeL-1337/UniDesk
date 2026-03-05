using ASP.Models;

namespace ASP.Services
{
    public interface ITicketService
    {
        IEnumerable<Ticket> GetAllTickets();
        void AddTicket(Ticket ticket);
        Ticket? GetTicketById(int id);
        IEnumerable<Ticket> SearchTickets(string search);
    }
}