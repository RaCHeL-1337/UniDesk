using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public class TicketService : ITicketService
    {
        private readonly UniDeskDbContext _context;

        public TicketService(UniDeskDbContext context)
        {
            _context = context;
        }

        public PagedResult<TicketListDto> GetAll(TicketQueryParameters parameters)
        {
            IQueryable<Ticket> query = _context.Tickets.AsNoTracking();

            if (parameters.Status.HasValue)
            {
                query = query.Where(t => t.Status == parameters.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                query = query.Where(t => t.Title.Contains(parameters.Search));
            }

            int totalCount = query.Count();

            query = parameters.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt);

            query = query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize);

            var items = query
                .Select(t => new TicketListDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt
                })
                .ToList();

            return new PagedResult<TicketListDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public IReadOnlyList<TicketReadDto> GetAllForApi()
        {
            return _context.Tickets
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => ToReadDto(t))
                .ToList();
        }

        public Ticket GetById(int id)
        {
            return _context.Tickets.FirstOrDefault(t => t.Id == id)
                ?? throw new EntityNotFoundException($"Ticket with id={id} was not found.");
        }

        public TicketReadDto Create(CreateTicketRequest request)
        {
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            return ToReadDto(ticket);
        }

        public void Update(int id, CreateTicketRequest request)
        {
            var ticket = GetById(id);

            ticket.Title = request.Title;
            ticket.Description = request.Description;

            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }

        public void UpdateStatus(int id, TicketStatus status)
        {
            var ticket = GetById(id);

            if (ticket.Status == TicketStatus.Closed)
            {
                throw new InvalidOperationException("Ticket is already closed.");
            }

            ticket.Status = status;
            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var ticket = GetById(id);

            _context.Tickets.Remove(ticket);
            _context.SaveChanges();
        }

        private static TicketReadDto ToReadDto(Ticket ticket)
        {
            return new TicketReadDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt
            };
        }
    }
}
