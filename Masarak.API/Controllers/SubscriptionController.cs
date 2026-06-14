using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.API.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    [Produces("application/json")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // POST /api/subscriptions/checkout
        [HttpPost("checkout")]
        [Authorize(Policy = AppPolicies.AnyAuthenticated)]
        [ProducesResponseType(typeof(CheckoutResult), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InitiateCheckout([FromBody] InitiateCheckoutRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _subscriptionService.InitiateCheckoutAsync(userId.Value, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/subscriptions/webhook
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = HttpContext.Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
                return BadRequest();

            try
            {
                await _subscriptionService.HandleStripeWebhookAsync(json, signature);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest();
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        // GET /api/subscriptions/me
        [HttpGet("me")]
        [Authorize(Policy = AppPolicies.AnyAuthenticated)]
        [ProducesResponseType(typeof(SubscriptionDto), 200)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> GetMyActiveSubscription()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var sub = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
            return sub != null ? Ok(sub) : NoContent();
        }

        // GET /api/subscriptions/me/history
        [HttpGet("me/history")]
        [Authorize(Policy = AppPolicies.AnyAuthenticated)]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), 200)]
        public async Task<IActionResult> GetMySubscriptionHistory()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var subs = await _subscriptionService.GetSubscriptionHistoryAsync(userId.Value);
            return Ok(subs);
        }

        // POST /api/subscriptions/admin/activate
        [HttpPost("admin/activate")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ProducesResponseType(typeof(SubscriptionDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AdminActivate([FromBody] AdminActivateRequest request)
        {
            var adminId = GetCurrentUserId();
            if (adminId == null) return Unauthorized();

            try
            {
                var result = await _subscriptionService.AdminActivateAsync(adminId.Value, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/subscriptions/admin/cancel/{id}
        [HttpPost("admin/cancel/{id}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AdminCancel(int id, [FromBody] AdminCancelRequest request)
        {
            var adminId = GetCurrentUserId();
            if (adminId == null) return Unauthorized();

            try
            {
                await _subscriptionService.AdminCancelAsync(adminId.Value, id, request.Reason);
                return Ok(new { message = "Subscription cancelled." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue("userid")
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
