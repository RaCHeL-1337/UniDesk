using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Models;
using UniDesk.Web.Services;

namespace UniDesk.Web.Controllers;

[ApiController]
[Route("api/tickets")]
[Tags("Tickets")]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsApiController(ITicketService ticketService)
    {
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
            var ticket = _ticketService.GetById(id);
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
            var dto = _ticketService.Create(request);

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
    public IActionResult Update(int id, [FromBody] CreateTicketRequest request)
    {
        try
        {
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
    public IActionResult UpdateStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        try
        {
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
}
