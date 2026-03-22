using UniDesk.Web.Models;

namespace UniDesk.Web.DTOs
{
    public class TicketReadDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}