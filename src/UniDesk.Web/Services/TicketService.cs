using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Models;

namespace UniDesk.Web.Services
{
    public class TicketService : ITicketService
    {
        private static readonly TimeSpan SlowDataOperationThreshold = TimeSpan.FromMilliseconds(100);

        private readonly UniDeskDbContext _context;
        private readonly ILogger<TicketService> _logger;

        public TicketService(UniDeskDbContext context, ILogger<TicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public PagedResult<TicketListDto> GetAll(TicketQueryParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();
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

            stopwatch.Stop();
            LogSlowDataOperation(
                "TicketsListQuery",
                stopwatch.Elapsed,
                new Dictionary<string, object?>
                {
                    ["Search"] = parameters.Search,
                    ["Status"] = parameters.Status,
                    ["SortOrder"] = parameters.SortOrder,
                    ["Page"] = parameters.Page,
                    ["PageSize"] = parameters.PageSize,
                    ["TotalCount"] = totalCount
                });

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
            var stopwatch = Stopwatch.StartNew();
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();
            stopwatch.Stop();

            _logger.LogInformation(
                "Ticket created {TicketId} {TicketTitle} {TicketStatus} {TicketCreatedAt} {ElapsedMilliseconds}",
                ticket.Id,
                ticket.Title,
                ticket.Status,
                ticket.CreatedAt,
                stopwatch.Elapsed.TotalMilliseconds);

            LogSlowDataOperation(
                "TicketCreate",
                stopwatch.Elapsed,
                new Dictionary<string, object?>
                {
                    ["TicketId"] = ticket.Id,
                    ["TicketTitle"] = ticket.Title,
                    ["TicketStatus"] = ticket.Status
                });

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

        private void LogSlowDataOperation(
            string operationName,
            TimeSpan elapsed,
            IReadOnlyDictionary<string, object?> details)
        {
            if (elapsed < SlowDataOperationThreshold)
            {
                return;
            }

            _logger.LogWarning(
                "Slow data operation {OperationName} took {ElapsedMilliseconds} ms with {@OperationDetails}",
                operationName,
                elapsed.TotalMilliseconds,
                details);
        }
    }
}
