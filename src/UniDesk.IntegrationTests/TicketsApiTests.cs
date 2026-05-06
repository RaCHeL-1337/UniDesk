using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;
using UniDesk.Web.Models;

namespace UniDesk.IntegrationTests;

public class TicketsApiTests
{
    [Fact]
    public async Task GetAll_returns_ok_and_list_of_tickets()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            db.Tickets.AddRange(
                new Ticket { Title = "Ticket A", Description = "Desc A", Status = TicketStatus.New },
                new Ticket { Title = "Ticket B", Description = "Desc B", Status = TicketStatus.InProgress }
            );
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/tickets?page=1&pageSize=10&sortOrder=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResult<TicketListDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.TotalCount);
        Assert.Equal(2, payload.Items.Count);
        Assert.Contains(payload.Items, i => i.Title == "Ticket A");
        Assert.Contains(payload.Items, i => i.Title == "Ticket B");
    }

    [Fact]
    public async Task Create_returns_created_and_can_be_fetched_by_id()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var createRequest = new CreateTicketRequest
        {
            Title = "New ticket",
            Description = "New description"
        };

        var createResponse = await client.PostAsJsonAsync("/api/tickets", createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketReadDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(createRequest.Title, created.Title);
        Assert.Equal(TicketStatus.New, created.Status);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.NotEqual(default, created.UpdatedAt);

        var getResponse = await client.GetAsync($"/api/tickets/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<TicketReadDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Status, fetched.Status);
    }

    [Fact]
    public async Task Create_with_invalid_input_returns_bad_request()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var invalidRequest = new CreateTicketRequest
        {
            Title = "",
            Description = ""
        };

        var response = await client.PostAsJsonAsync("/api/tickets", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Contains(nameof(CreateTicketRequest.Title), problem.Errors.Keys);
        Assert.Contains(nameof(CreateTicketRequest.Description), problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_with_too_long_api_input_returns_bad_request()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var invalidRequest = new CreateTicketRequest
        {
            Title = new string('T', 101),
            Description = new string('D', 501)
        };

        var response = await client.PostAsJsonAsync("/api/tickets", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(CreateTicketRequest.Title), problem!.Errors.Keys);
        Assert.Contains(nameof(CreateTicketRequest.Description), problem.Errors.Keys);
    }

    [Fact]
    public async Task Mvc_create_form_contains_antiforgery_token_and_rejects_invalid_token()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var formResponse = await client.GetAsync("/Tickets/Create");
        formResponse.EnsureSuccessStatusCode();

        var formHtml = await formResponse.Content.ReadAsStringAsync();
        Assert.Contains("__RequestVerificationToken", formHtml);

        var invalidPost = await client.PostAsync("/Tickets/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = "invalid-token",
            ["Title"] = "CSRF check",
            ["Description"] = "Invalid CSRF token should be rejected"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, invalidPost.StatusCode);
    }

    [Fact]
    public async Task App_adds_security_headers()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions));
        Assert.Contains("nosniff", contentTypeOptions);
        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var frameOptions));
        Assert.Contains("DENY", frameOptions);
    }

    [Fact]
    public async Task GetById_for_missing_ticket_returns_404_problem_details()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/tickets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
        Assert.Equal("Ticket not found", problem.Title);
    }

    [Fact]
    public async Task UpdateStatus_changes_ticket_status()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            var ticket = new Ticket { Title = "To close", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var updateRequest = new UpdateTicketStatusRequest
        {
            Status = TicketStatus.Closed
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<TicketReadDto>();
        Assert.NotNull(fetched);
        Assert.Equal(TicketStatus.Closed, fetched!.Status);
    }
}
