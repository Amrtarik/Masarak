using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Masarak.Application.Interfaces;
using Masarak.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Masarak.API.Extensions
{
    public class SubscriptionAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _env;

        public SubscriptionAccessMiddleware(RequestDelegate next, IHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bypass if dev header is present
            if (_env.IsDevelopment() && context.Request.Headers.TryGetValue("X-Dev-Bypass-Subscription", out var devBypass) && devBypass == "true")
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // Skip for non-authenticated routes or routes that don't require subscription gating
            if (path.StartsWith("/api/auth") ||
                path.StartsWith("/api/subscriptions/webhook") ||
                path.StartsWith("/api/plans") ||
                path.StartsWith("/api/subscriptions/checkout") ||
                path.StartsWith("/api/subscriptions/me") ||
                path.StartsWith("/api/parent/link-student") ||
                path.StartsWith("/api/parent/linked-students"))
            {
                await _next(context);
                return;
            }

            // Must be authenticated to reach this point for academic routes
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            var role = context.User.FindFirstValue("role") ?? context.User.FindFirstValue(ClaimTypes.Role);
            
            // Admins bypass subscription checks
            if (role == AppRoles.Admin)
            {
                await _next(context);
                return;
            }

            // Parents access is generally allowed if they just want to view linked children stats,
            // but if they try to access specific child academic endpoints, it will be validated in the controller.
            // For now, let's enforce subscription gating mostly on Student & Teacher routes.
            if (role == AppRoles.Parent)
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirstValue("userid")
                              ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out var userId))
            {
                // We resolve this from scope because it's a scoped service
                var subscriptionAccessService = context.RequestServices.GetRequiredService<ISubscriptionAccessService>();
                
                var hasActiveSubscription = await subscriptionAccessService.HasActiveSubscriptionAsync(userId, context.RequestAborted);
                
                if (!hasActiveSubscription)
                {
                    context.Response.StatusCode = 402; // Payment Required
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        code = "SUBSCRIPTION_REQUIRED",
                        message = "An active subscription is required to access this resource.",
                        checkoutUrl = "/plans"
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }
            }

            await _next(context);
        }
    }
}
