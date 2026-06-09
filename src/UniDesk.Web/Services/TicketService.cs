using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Exceptions;
using UniDesk.Web.Models;
using UniDesk.Web.Options;

namespace UniDesk.Web.Services
{
    public class TicketService : ITicketService
    {
        private readonly DiagnosticsOptions _diagnosticsOptions;
        private readonly UniDeskDbContext _context;
        private readonly ILogger<TicketService> _logger;
        private readonly IMarkdownRenderer _markdownRenderer;

        public TicketService(
            UniDeskDbContext context,
            ILogger<TicketService> logger,
            IOptions<DiagnosticsOptions> diagnosticsOptions,
            IMarkdownRenderer markdownRenderer)
        {
            _context = context;
            _logger = logger;
            _diagnosticsOptions = diagnosticsOptions.Value;
            _markdownRenderer = markdownRenderer;
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

        public TicketReadDto GetById(int id)
        {
            return ToReadDto(GetTicketEntity(id));
        }

        public TicketDetailsDto GetDetails(int id)
        {
            var ticket = _context.Tickets
                .AsNoTracking()
                .Include(t => t.Comments)
                .FirstOrDefault(t => t.Id == id)
                ?? throw new EntityNotFoundException($"Ticket with id={id} was not found.");

            return ToDetailsDto(ticket);
        }

        private Ticket GetTicketEntity(int id)
        {
            return _context.Tickets.FirstOrDefault(t => t.Id == id)
                ?? throw new EntityNotFoundException($"Ticket with id={id} was not found.");
        }

        public TicketReadDto Create(
            CreateTicketRequest request,
            string authorId = "system",
            string authorEmail = "system@unidesk.local")
        {
            var stopwatch = Stopwatch.StartNew();
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                CreatedByUserId = authorId,
                CreatedByEmail = authorEmail
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

        public TicketCommentDto AddComment(
            int ticketId,
            CreateTicketCommentRequest request,
            string authorId,
            string authorEmail)
        {
            var ticket = GetTicketEntity(ticketId);

            var comment = new TicketComment
            {
                TicketId = ticket.Id,
                AuthorId = authorId,
                AuthorEmail = authorEmail,
                Message = request.Message.Trim()
            };

            _context.TicketComments.Add(comment);
            _context.SaveChanges();

            _logger.LogInformation(
                "Ticket comment created {TicketId} {CommentId} {AuthorId} {AuthorEmail}",
                ticket.Id,
                comment.Id,
                comment.AuthorId,
                comment.AuthorEmail);

            return ToCommentDto(comment);
        }

        public void Update(int id, CreateTicketRequest request)
        {
            var ticket = GetTicketEntity(id);

            ticket.Title = request.Title;
            ticket.Description = request.Description;

            _context.Tickets.Update(ticket);
            _context.SaveChanges();
        }

        public void UpdateStatus(int id, TicketStatus status)
        {
            var ticket = GetTicketEntity(id);

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
            var ticket = GetTicketEntity(id);

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

        private TicketDetailsDto ToDetailsDto(Ticket ticket)
        {
            return new TicketDetailsDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByEmail = ticket.CreatedByEmail,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                Comments = ticket.Comments
                    .OrderBy(comment => comment.CreatedAt)
                    .Select(ToCommentDto)
                    .ToList()
            };
        }

        private TicketCommentDto ToCommentDto(TicketComment comment)
        {
            return new TicketCommentDto
            {
                Id = comment.Id,
                AuthorId = comment.AuthorId,
                AuthorEmail = comment.AuthorEmail,
                Message = comment.Message,
                MessageHtml = _markdownRenderer.ToSafeHtml(comment.Message),
                CreatedAt = comment.CreatedAt
            };
        }

        private void LogSlowDataOperation(
            string operationName,
            TimeSpan elapsed,
            IReadOnlyDictionary<string, object?> details)
        {
            var threshold = TimeSpan.FromMilliseconds(_diagnosticsOptions.SlowDataOperationThresholdMilliseconds);

            if (elapsed < threshold)
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
