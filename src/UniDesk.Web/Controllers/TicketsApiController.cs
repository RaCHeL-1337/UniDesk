using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;
using UniDesk.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace UniDesk.Web.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketsApiController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsApiController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TicketReadDto>> GetAll()
        {
            var tickets = _ticketService.GetAll()
                .Select(t => new TicketReadDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToList();

            return Ok(tickets);
        }

        [HttpGet("{id}")]
        public ActionResult<TicketReadDto> GetById(int id)
        {
            var ticket = _ticketService.GetById(id);

            if (ticket == null)
                return NotFound();

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
        public ActionResult<TicketReadDto> Create([FromBody] CreateTicketRequest request)
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
                return BadRequest(new
                {
                    error = "Błąd zapisu do bazy danych"
                });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] CreateTicketRequest request)
        {
            try
            {
                var ticket = _ticketService.GetById(id);

                if (ticket == null)
                    return NotFound();

                ticket.Title = request.Title;
                ticket.Description = request.Description;

                _ticketService.Update(ticket);

                return NoContent();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    error = "Błąd aktualizacji danych"
                });
            }
        }
    }
}