using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    [Fact]
    public async Task Anonymous_mvc_tickets_redirects_to_login()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/Tickets");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Anonymous_controller_api_returns_unauthorized()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_controller_api_delete_returns_unauthorized()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.DeleteAsync("/api/tickets/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_without_admin_role_cannot_delete_ticket()
    {
        await using var factory = new UniDeskWebApplicationFactory(testRoles: Array.Empty<string>());
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            var ticket = new Ticket { Title = "Role protected", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        Assert.NotNull(await verifyDb.Tickets.FindAsync(ticketId));
    }

    [Fact]
    public async Task Admin_user_can_delete_ticket_through_controller_api()
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
            var ticket = new Ticket { Title = "Admin delete", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.DeleteAsync($"/api/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        Assert.Null(await verifyDb.Tickets.FindAsync(ticketId));
    }

    [Fact]
    public async Task Non_admin_user_does_not_see_delete_button_in_ticket_details()
    {
        await using var factory = new UniDeskWebApplicationFactory(testRoles: Array.Empty<string>());
        var client = factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
            var ticket = new Ticket { Title = "UI delete", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.GetAsync($"/Tickets/Details/{ticketId}");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Usun zgloszenie", html);
    }

    [Fact]
    public async Task Admin_user_sees_delete_button_in_ticket_details()
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
            var ticket = new Ticket { Title = "Admin UI delete", Description = "Desc", Status = TicketStatus.New };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var response = await client.GetAsync($"/Tickets/Details/{ticketId}");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usun zgloszenie", html);
    }

    [Fact]
    public async Task Register_rejects_password_without_special_character()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var registerPageResponse = await client.GetAsync("/Account/Register");
        registerPageResponse.EnsureSuccessStatusCode();

        var registerHtml = await registerPageResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(registerHtml, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success);

        var registerResponse = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value,
            ["Email"] = "weak-password@unidesk.local",
            ["OrganizationName"] = "Weak Password Org",
            ["Password"] = "Admin123",
            ["ConfirmPassword"] = "Admin123"
        }));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.False(registerResponse.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.Contains(".AspNetCore.Identity.Application")));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        Assert.False(db.Users.Any(user => user.Email == "weak-password@unidesk.local"));
    }

    [Fact]
    public async Task Login_sets_identity_cookie_and_allows_tickets_view()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var loginPageResponse = await client.GetAsync("/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();

        var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(loginHtml, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success);

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value,
            ["Email"] = "admin@unidesk.local",
            ["Password"] = "Admin123!",
            ["RememberMe"] = "false"
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains(".AspNetCore.Identity.Application"));

        var ticketsResponse = await client.GetAsync("/Tickets");
        Assert.Equal(HttpStatusCode.OK, ticketsResponse.StatusCode);
    }

    [Fact]
    public async Task Identity_api_register_rejects_password_without_special_character()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/register", new
        {
            email = "api-weak-password@unidesk.local",
            password = "Admin123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UniDeskDbContext>();
        Assert.False(db.Users.Any(user => user.Email == "api-weak-password@unidesk.local"));
    }

    [Fact]
    public async Task Identity_api_login_with_cookies_sets_identity_cookie()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsJsonAsync("/login?useCookies=true", new
        {
            email = "admin@unidesk.local",
            password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains(".AspNetCore.Identity.Application"));
    }

    [Fact]
    public async Task Health_endpoints_return_healthy_without_authentication()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal("Healthy", await liveResponse.Content.ReadAsStringAsync());

        using var readyJson = JsonDocument.Parse(await readyResponse.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", readyJson.RootElement.GetProperty("status").GetString());
        Assert.True(readyJson.RootElement.TryGetProperty("checks", out var checks));
        Assert.Contains(checks.EnumerateArray(), check =>
            check.GetProperty("name").GetString() == "sqlite"
            && check.GetProperty("status").GetString() == "Healthy");
    }

    [Fact]
    public async Task Correlation_id_is_returned_for_each_request()
    {
        await using var factory = new UniDeskWebApplicationFactory(useTestAuthentication: false);
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        const string correlationId = "test-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Contains(correlationId, values);
    }
}
