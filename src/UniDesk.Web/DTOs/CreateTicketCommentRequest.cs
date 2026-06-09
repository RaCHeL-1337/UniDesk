using System.ComponentModel.DataAnnotations;

namespace UniDesk.Web.DTOs;

public class CreateTicketCommentRequest
{
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
}
