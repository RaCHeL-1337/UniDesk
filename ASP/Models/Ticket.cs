using System;
using System.ComponentModel.DataAnnotations;

namespace ASP.Models
{
    public enum TicketStatus
    {
        Open,
        InProgress,
        Closed
    }

    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}