using System.ComponentModel.DataAnnotations;

namespace UniDesk.Web.DTOs
{
    public class CreateTicketRequest
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
