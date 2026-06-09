namespace UniDesk.Web.DTOs;

public class TicketCommentDto
{
    public int Id { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageHtml { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
