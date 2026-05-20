using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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

    [Fact]
    public async Task Mvc_update_status_changes_ticket_status()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            var ticket = new Ticket { Title = "MVC status", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var detailsResponse = await client.GetAsync($"/Tickets/Details/{ticketId}");
        detailsResponse.EnsureSuccessStatusCode();

        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(detailsHtml, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success);

        var updateResponse = await client.PostAsync($"/Tickets/UpdateStatus/{ticketId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value,
            ["Status"] = TicketStatus.InProgress.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, updateResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            var updated = await db.Tickets.FindAsync(ticketId);
            Assert.NotNull(updated);
            Assert.Equal(TicketStatus.InProgress, updated!.Status);
        }
    }

    [Fact]
    public async Task MinimalApiV2_get_returns_ok_and_list_of_tickets()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            db.Tickets.Add(new Ticket { Title = "V2 ticket", Description = "Desc", Status = TicketStatus.New });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v2/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<TicketReadDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload!, t => t.Title == "V2 ticket");
    }

    [Fact]
    public async Task MinimalApiV2_post_creates_ticket()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new CreateTicketRequest
        {
            Title = "Created through v2",
            Description = "Desc"
        };

        var response = await client.PostAsJsonAsync("/api/v2/tickets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TicketReadDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        var stored = await db.Tickets.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(request.Title, stored!.Title);
    }

    [Fact]
    public async Task MinimalApiV2_put_updates_ticket()
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
            var ticket = new Ticket { Title = "Before", Description = "Before desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var request = new CreateTicketRequest
        {
            Title = "After",
            Description = "After desc"
        };

        var response = await client.PutAsJsonAsync($"/api/v2/tickets/{ticketId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        var updated = await verifyDb.Tickets.FindAsync(ticketId);
        Assert.NotNull(updated);
        Assert.Equal(request.Title, updated!.Title);
        Assert.Equal(request.Description, updated.Description);
    }

    [Fact]
    public async Task MinimalApiV2_delete_removes_ticket()
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
            var ticket = new Ticket { Title = "Delete me", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.DeleteAsync($"/api/v2/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        var deleted = await verifyDb.Tickets.FindAsync(ticketId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task MinimalApiV2_delete_missing_ticket_returns_problem_details()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.DeleteAsync("/api/v2/tickets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
        Assert.Equal("Entity not found", problem.Title);
    }

    [Fact]
    public async Task MinimalApiV2_put_missing_ticket_returns_problem_details()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new CreateTicketRequest
        {
            Title = "Missing",
            Description = "Missing"
        };

        var response = await client.PutAsJsonAsync("/api/v2/tickets/999999", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
        Assert.Equal("Entity not found", problem.Title);
    }

    [Fact]
    public async Task MinimalApiV2_invalid_json_returns_problem_details()
    {
        await using var factory = new UniDeskWebApplicationFactory();
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var content = new StringContent("{title:\"Broken\"}", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v2/tickets", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Equal("Invalid request", problem.Title);
    }
}
