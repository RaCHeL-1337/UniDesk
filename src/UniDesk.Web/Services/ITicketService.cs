using UniDesk.Web.Models;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Services
{
    public interface ITicketService
    {
        PagedResult<TicketListDto> GetAll(TicketQueryParameters parameters);
        Ticket? GetById(int id);
        void Add(Ticket ticket);
        void Update(Ticket ticket);
    }
}