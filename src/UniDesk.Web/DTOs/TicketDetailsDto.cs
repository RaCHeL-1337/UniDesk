using UniDesk.Web.Models;

namespace UniDesk.Web.DTOs;

public class TicketDetailsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool CanParticipateInDiscussion { get; set; }
    public bool CanManageTicket { get; set; }
    public IReadOnlyList<TicketCommentDto> Comments { get; set; } = new List<TicketCommentDto>();
}
