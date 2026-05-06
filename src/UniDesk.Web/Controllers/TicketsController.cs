using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly TicketService _domainTicketService;

        public TicketsController(ITicketService ticketService, TicketService domainTicketService)
        {
            _ticketService = ticketService;
            _domainTicketService = domainTicketService;
        }

        [HttpGet]
        public IActionResult Index([FromQuery] TicketQueryParameters parameters)
        {
            var result = _ticketService.GetAll(parameters);

            ViewBag.TotalCount = result.TotalCount;
            ViewBag.Search = parameters.Search;
            ViewBag.Status = parameters.Status?.ToString();
            ViewBag.SortOrder = parameters.SortOrder;
            ViewBag.PageSize = parameters.PageSize;

            return View(result.Items);
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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description
            };

            _ticketService.Add(ticket);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, UpdateTicketStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusError"] = "Nieprawidlowy status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var ticket = _ticketService.GetById(id);

            if (ticket == null)
            {
                return NotFound();
            }

            try
            {
                _domainTicketService.UpdateStatus(ticket, request.Status!.Value);
                _ticketService.Update(ticket);
                TempData["StatusMessage"] = "Status zaktualizowany.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["StatusError"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
