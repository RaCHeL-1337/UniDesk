using UniDesk.Web.Models;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Services
{
    public interface ITicketService
    {
        PagedResult<TicketListDto> GetAll(TicketQueryParameters parameters);
        IReadOnlyList<TicketReadDto> GetAllForApi();
        TicketReadDto GetById(int id);
        TicketDetailsDto GetDetails(int id);
        TicketReadDto Create(
            CreateTicketRequest request,
            string authorId = "system",
            string authorEmail = "system@unidesk.local");
        TicketCommentDto AddComment(int ticketId, CreateTicketCommentRequest request, string authorId, string authorEmail);
        void Update(int id, CreateTicketRequest request);
        void UpdateStatus(int id, TicketStatus status);
        void Delete(int id);
    }
}
