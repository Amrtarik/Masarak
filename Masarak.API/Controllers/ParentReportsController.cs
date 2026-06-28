using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.API.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/parent")]
    [Authorize(Policy = AppPolicies.ParentOnly)]
    public class ParentReportsController : ControllerBase
    {
        private readonly IAiAnalyticsService _aiService;

        public ParentReportsController(IAiAnalyticsService aiService) => _aiService = aiService;

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

        /// <summary>Get a cached parent report for a linked student.</summary>
        [HttpGet("reports/{studentId}/{month}")]
        public async Task<IActionResult> GetParentReport(
            int studentId, string month, CancellationToken ct = default)
        {
            var result = await _aiService.GetParentReportAsync(GetUserId(), studentId, month, ct);
            if (result == null) return NotFound(new { message = "Report not found. Generate one first." });
            return Ok(result);
        }

        /// <summary>Generate a fresh parent report on demand (cached for 24 hours).</summary>
        [HttpPost("reports/{studentId}/generate")]
        public async Task<IActionResult> GenerateParentReport(
            int studentId, [FromBody] GenerateParentReportRequest request, CancellationToken ct = default)
        {
            var result = await _aiService.GenerateParentReportAsync(
                GetUserId(), studentId, request.ReportMonth, ct);
            return Ok(result);
        }

        /// <summary>Get active performance alerts for a linked student.</summary>
        [HttpGet("children/{studentId}/alerts")]
        public async Task<IActionResult> GetStudentAlerts(
            int studentId, CancellationToken ct = default)
        {
            var result = await _aiService.GetStudentAlertsForParentAsync(GetUserId(), studentId, ct);
            return Ok(result);
        }
    }
}
