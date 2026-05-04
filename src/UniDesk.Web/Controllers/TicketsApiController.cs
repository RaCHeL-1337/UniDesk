using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers;

[ApiController]
[Route("api/tickets")]
[Tags("Tickets")]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly TicketService _domainTicketService;

    public TicketsApiController(ITicketService ticketService, TicketService domainTicketService)
    {
        _ticketService = ticketService;
        _domainTicketService = domainTicketService;
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
        var ticket = _ticketService.GetById(id);

        if (ticket == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found",
                detail: $"Ticket with id={id} was not found.");
        }

        var dto = new TicketReadDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description
            };

            _ticketService.Add(ticket);

            var dto = new TicketReadDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, dto);
        }
        catch (DbUpdateException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Błąd zapisu do bazy danych");
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, [FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = _ticketService.GetById(id);

            if (ticket == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Ticket not found",
                    detail: $"Ticket with id={id} was not found.");
            }

            ticket.Title = request.Title;
            ticket.Description = request.Description;

            _ticketService.Update(ticket);

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Błąd aktualizacji danych");
        }
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        try
        {
            var ticket = _ticketService.GetById(id);

            if (ticket == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Ticket not found",
                    detail: $"Ticket with id={id} was not found.");
            }

            _domainTicketService.UpdateStatus(ticket, request.Status!.Value);
            _ticketService.Update(ticket);

            return NoContent();
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
                title: "Błąd aktualizacji danych");
        }
    }
}
