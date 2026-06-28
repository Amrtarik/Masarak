using Masarak.API.Policies;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    /// <summary>
    /// Phase 6: Admin content moderation endpoint.
    /// Allows admin to deactivate inappropriate content items.
    /// </summary>
    [ApiController]
    [Route("api/admin/content")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminContentModerationController : ControllerBase
    {
        private readonly IContentItemRepository _contentRepo;

        public AdminContentModerationController(IContentItemRepository contentRepo)
        {
            _contentRepo = contentRepo;
        }

        /// <summary>
        /// PUT /api/admin/content/{id}/moderate — Deactivate a content item for moderation.
        /// </summary>
        [HttpPut("{id}/moderate")]
        public async Task<IActionResult> ModerateContent(int id, [FromBody] ModerateContentRequest? request = null, CancellationToken ct = default)
        {
            var contentItem = await _contentRepo.GetByIdAsync(id, ct);
            if (contentItem == null)
                return NotFound(new { Code = "CONTENT_NOT_FOUND", Message = $"Content item with ID {id} not found." });

            contentItem.Deactivate();
            await _contentRepo.UpdateAsync(contentItem, ct);

            return NoContent();
        }
    }

    /// <summary>Request body for moderating content.</summary>
    public class ModerateContentRequest
    {
        public string? Reason { get; set; }
    }
}
