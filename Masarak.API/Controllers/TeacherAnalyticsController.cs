using Masarak.Application.Interfaces;
using Masarak.API.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/teacher")]
    [Authorize(Policy = AppPolicies.TeacherOnly)]
    public class TeacherAnalyticsController : ControllerBase
    {
        private readonly IAiAnalyticsService _aiService;

        public TeacherAnalyticsController(IAiAnalyticsService aiService) => _aiService = aiService;

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

        /// <summary>Class analytics dashboard: average, distribution, top/bottom 5, session stats.</summary>
        [HttpGet("analytics/{classId}/{subjectId}")]
        public async Task<IActionResult> GetClassAnalytics(
            int classId, int subjectId, [FromQuery] int academicYear = 2026, CancellationToken ct = default)
        {
            var result = await _aiService.GetClassAnalyticsAsync(GetUserId(), classId, subjectId, academicYear, ct);
            return Ok(result);
        }

        /// <summary>Get per-student insight including weak topics, alerts, and latest suggestion.</summary>
        [HttpGet("students/{studentId}/insights/{subjectId}")]
        public async Task<IActionResult> GetStudentInsight(
            int studentId, int subjectId, CancellationToken ct = default)
        {
            var result = await _aiService.GetStudentInsightAsync(GetUserId(), studentId, subjectId, ct);
            return Ok(result);
        }


        /// <summary>Generate AI teaching suggestion for a specific student in a subject.</summary>
        [HttpPost("students/{studentId}/suggestions/{subjectId}")]
        public async Task<IActionResult> GenerateTeachingSuggestion(
            int studentId, int subjectId, CancellationToken ct = default)
        {
            var result = await _aiService.GenerateTeachingSuggestionAsync(GetUserId(), studentId, subjectId, ct);
            return Ok(result);
        }
    }
}
