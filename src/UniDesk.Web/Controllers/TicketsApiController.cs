using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Authorization;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
[Tags("Tickets")]
public class TicketsApiController : ControllerBase
{
    private static readonly TicketDiscussionRequirement TicketAccessRequirement = new();

    private readonly IAuthorizationService _authorizationService;
    private readonly ITicketService _ticketService;

    public TicketsApiController(
        ITicketService ticketService,
        IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
        _ticketService = ticketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TicketListDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] TicketQueryParameters parameters)
    {
        var result = _ticketService.GetAll(parameters);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TicketReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        try
        {
            return Ok(_ticketService.GetById(id));
        }
        catch (EntityNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found",
                detail: $"Ticket with id={id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateTicketRequest request)
    {
        try
        {
            var dto = _ticketService.Create(request, GetCurrentUserId(), GetCurrentUserEmail());

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (DbUpdateException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Blad zapisu do bazy danych");
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = _ticketService.GetDetails(id);
            var authorization = await _authorizationService.AuthorizeAsync(
                User,
                ticket,
                TicketAccessRequirement);

            if (!authorization.Succeeded)
            {
                return Forbid();
            }

            _ticketService.Update(id, request);

            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found",
                detail: $"Ticket with id={id} was not found.");
        }
        catch (DbUpdateException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Blad aktualizacji danych");
        }
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        try
        {
            var ticket = _ticketService.GetDetails(id);
            var authorization = await _authorizationService.AuthorizeAsync(
                User,
                ticket,
                TicketAccessRequirement);

            if (!authorization.Succeeded)
            {
                return Forbid();
            }

            _ticketService.UpdateStatus(id, request.Status!.Value);

            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found",
                detail: $"Ticket with id={id} was not found.");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid ticket status change",
                detail: ex.Message);
        }
        catch (DbUpdateException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Blad aktualizacji danych");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        try
        {
            _ticketService.Delete(id);
            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found",
                detail: $"Ticket with id={id} was not found.");
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
