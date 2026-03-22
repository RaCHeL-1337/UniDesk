using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public interface ITicketService
    {
        IEnumerable<Ticket> GetAll();
        Ticket? GetById(int id);
        void Add(Ticket ticket);
        void Update(Ticket ticket);
    }
}