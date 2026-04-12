using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Data;
using UniDesk.Web.Models;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Services
{
    public class DbTicketService : ITicketService
    {
        private readonly UniDeskDbContext _context;

        public DbTicketService(UniDeskDbContext context)
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

        public Ticket? GetById(int id)
        {
            return _context.Tickets.FirstOrDefault(t => t.Id == id);
        }

        public void Add(Ticket ticket)
        {
            ticket.CreatedAt = DateTime.Now;
            ticket.UpdatedAt = DateTime.Now;

            _context.Tickets.Add(ticket);
            _context.SaveChanges();
        }

        public void Update(Ticket ticket)
        {
            ticket.UpdatedAt = DateTime.Now;

            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }
    }
}