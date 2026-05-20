using UniDesk.Web.Models;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Services
{
    public interface ITicketService
    {
        PagedResult<TicketListDto> GetAll(TicketQueryParameters parameters);
        IReadOnlyList<TicketReadDto> GetAllForApi();
        Ticket GetById(int id);
        TicketReadDto Create(CreateTicketRequest request);
        void Update(int id, CreateTicketRequest request);
        void UpdateStatus(int id, TicketStatus status);
        void Delete(int id);
    }
}
