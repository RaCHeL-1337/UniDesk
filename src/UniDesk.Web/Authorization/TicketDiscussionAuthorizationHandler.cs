using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using UniDesk.Web.Data;
using UniDesk.Web.DTOs;

namespace UniDesk.Web.Authorization;

public class TicketDiscussionAuthorizationHandler
    : AuthorizationHandler<TicketDiscussionRequirement, TicketDetailsDto>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TicketDiscussionRequirement requirement,
        TicketDetailsDto resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(resource.CreatedByUserId, userId, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
