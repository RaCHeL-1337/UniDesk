using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index(string? search)
        {
            ViewBag.Search = search;

            var tickets = _ticketService.GetAll();
            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            _ticketService.Add(ticket);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var ticket = _ticketService.GetById(id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
    }
}