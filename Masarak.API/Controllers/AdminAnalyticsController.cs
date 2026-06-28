using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.API.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class AdminAnalyticsController : ControllerBase
    {
        private readonly IAiAnalyticsService _aiService;

        public AdminAnalyticsController(IAiAnalyticsService aiService) => _aiService = aiService;

        private string GetUserEmail() => User.FindFirstValue(ClaimTypes.Email) ?? "admin";

        /// <summary>Platform-wide analytics: students, teachers, subscriptions, revenue, session rate.</summary>
        [HttpGet("analytics/platform")]
        public async Task<IActionResult> GetPlatformAnalytics(
            [FromQuery] int academicYear = 2026, CancellationToken ct = default)
        {
            var result = await _aiService.GetPlatformAnalyticsAsync(academicYear, ct);
            return Ok(result);
        }

        /// <summary>Grade-level performance heatmap with color-coded subject scores.</summary>
        [HttpGet("analytics/grades/{gradeId}/heatmap")]
        public async Task<IActionResult> GetGradeHeatmap(
            int gradeId, [FromQuery] int academicYear = 2026, CancellationToken ct = default)
        {
            var result = await _aiService.GetGradeHeatmapAsync(gradeId, academicYear, ct);
            return Ok(result);
        }

        /// <summary>List all AI prompt templates.</summary>
        [HttpGet("ai/prompt-templates")]
        public async Task<IActionResult> GetAllPromptTemplates(CancellationToken ct = default)
        {
            var result = await _aiService.GetAllPromptTemplatesAsync(ct);
            return Ok(result);
        }

        /// <summary>Update an AI prompt template by key.</summary>
        [HttpPut("ai/prompt-templates/{key}")]
        public async Task<IActionResult> UpdatePromptTemplate(
            string key, [FromBody] UpdatePromptTemplateRequest request, CancellationToken ct = default)
        {
            await _aiService.UpdatePromptTemplateAsync(key, request, GetUserEmail(), ct);
            return NoContent();
        }
    }
}
