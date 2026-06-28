using Masarak.Application.Interfaces;
using Masarak.API.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/student")]
    [Authorize(Policy = AppPolicies.StudentOnly)]
    public class StudentInsightsController : ControllerBase
    {
        private readonly IAiAnalyticsService _aiService;

        public StudentInsightsController(IAiAnalyticsService aiService) => _aiService = aiService;

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

        /// <summary>Student learning insights dashboard: weakness analyses, recommendations, alerts, trends.</summary>
        [HttpGet("insights")]
        public async Task<IActionResult> GetMyLearningInsights(
            [FromQuery] int academicYear = 2026, CancellationToken ct = default)
        {
            var result = await _aiService.GetLearningInsightsAsync(GetUserId(), academicYear, ct);
            return Ok(result);
        }

        /// <summary>Get AI-recommended content for a specific subject.</summary>
        [HttpGet("recommendations/{subjectId}")]
        public async Task<IActionResult> GetMyRecommendedContent(
            int subjectId, CancellationToken ct = default)
        {
            var result = await _aiService.GetRecommendedContentAsync(GetUserId(), subjectId, ct);
            return Ok(result);
        }
    }
}
