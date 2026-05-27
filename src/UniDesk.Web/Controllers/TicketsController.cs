using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers
{
    [Authorize]
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
            ViewBag.Search = parameters.Search;
            ViewBag.Status = parameters.Status?.ToString();
            ViewBag.SortOrder = parameters.SortOrder;
            ViewBag.Page = parameters.Page;
            ViewBag.PageSize = parameters.PageSize;

            return View(result.Items);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                var ticket = _ticketService.GetById(id);
                return View(ticket);
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
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

            _ticketService.Create(request);

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

            try
            {
                _ticketService.UpdateStatus(id, request.Status!.Value);
                TempData["StatusMessage"] = "Status zaktualizowany.";
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["StatusError"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                _ticketService.Delete(id);
                TempData["StatusMessage"] = "Zgloszenie usuniete.";
                return RedirectToAction(nameof(Index));
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
