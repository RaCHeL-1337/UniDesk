using UniDesk.Web.Models;

namespace UniDesk.Web.DTOs
{
    public class TicketQueryParameters
    {
        public string? Search { get; set; }
        public TicketStatus? Status { get; set; }
        public string? SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
