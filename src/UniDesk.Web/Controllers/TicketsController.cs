using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

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
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(CreateTicketRequest request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description
            };

            _ticketService.Add(ticket);

            return RedirectToAction("Index");
        }
    }
}