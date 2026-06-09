using System.ComponentModel.DataAnnotations;

namespace UniDesk.Web.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    [Required]
    [StringLength(450)]
    public string AuthorId { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string AuthorEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
