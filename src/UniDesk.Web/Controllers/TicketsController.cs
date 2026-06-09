using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.Authorization;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private static readonly TicketDiscussionRequirement DiscussionRequirement = new();

        private readonly IAuthorizationService _authorizationService;
        private readonly ITicketService _ticketService;

        public TicketsController(
            ITicketService ticketService,
            IAuthorizationService authorizationService)
        {
            _ticketService = ticketService;
            _authorizationService = authorizationService;
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
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var ticket = _ticketService.GetDetails(id);
                var authorization = await _authorizationService.AuthorizeAsync(
                    User,
                    ticket,
                    DiscussionRequirement);

                ticket.CanParticipateInDiscussion = authorization.Succeeded;
                ticket.CanManageTicket = authorization.Succeeded;
                if (!authorization.Succeeded)
                {
                    ticket.Comments = Array.Empty<TicketCommentDto>();
                }

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

            _ticketService.Create(request, GetCurrentUserId(), GetCurrentUserEmail());

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusError"] = "Nieprawidlowy status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var ticket = _ticketService.GetDetails(id);
                var authorization = await _authorizationService.AuthorizeAsync(
                    User,
                    ticket,
                    DiscussionRequirement);

                if (!authorization.Succeeded)
                {
                    return Forbid();
                }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, CreateTicketCommentRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            {
                TempData["CommentError"] = "Komentarz nie moze byc pusty i musi miec maksymalnie 1000 znakow.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var ticket = _ticketService.GetDetails(id);
                var authorization = await _authorizationService.AuthorizeAsync(
                    User,
                    ticket,
                    DiscussionRequirement);

                if (!authorization.Succeeded)
                {
                    return Forbid();
                }

                _ticketService.AddComment(id, request, GetCurrentUserId(), GetCurrentUserEmail());
                TempData["CommentMessage"] = "Komentarz dodany.";
            }
            catch (EntityNotFoundException)
            {
                return NotFound();
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

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown-user";
        }

        private string GetCurrentUserEmail()
        {
            return User.Identity?.Name ?? "unknown@unidesk.local";
        }
    }
}
