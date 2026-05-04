using System.ComponentModel.DataAnnotations;
using UniDesk.Web.Models;

namespace UniDesk.Web.DTOs;

public class UpdateTicketStatusRequest
{
    [Required]
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus? Status { get; set; }
}

