using System.ComponentModel.DataAnnotations;

namespace UniDesk.Web.Models
{
    public enum TicketStatus
    {
        New,
        InProgress,
        Closed
    }

    public class Ticket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        [StringLength(100, ErrorMessage = "Tytuł może mieć maksymalnie 100 znaków.")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany.")]
        [StringLength(500, ErrorMessage = "Opis może mieć maksymalnie 500 znaków.")]
        public required string Description { get; set; }

        [Required]
        [StringLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string CreatedByEmail { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    }
}
