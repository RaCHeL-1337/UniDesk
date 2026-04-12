using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.Services;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public IActionResult Index([FromQuery] TicketQueryParameters parameters)
        {
            var result = _ticketService.GetAll(parameters);

            ViewBag.TotalCount = result.TotalCount;

            return View(result.Items);
        }
    }
}