using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/admin/performance")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    [Produces("application/json")]
    public class AdminPerformanceController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;

        public AdminPerformanceController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        [HttpGet("classes/{classId}/subjects/{subjectId}")]
        [ProducesResponseType(typeof(ClassPerformanceReportDto), 200)]
        public async Task<IActionResult> GetClassReport(int classId, int subjectId, [FromQuery] string academicYear, CancellationToken ct)
        {
            var report = await _assessmentService.GetClassPerformanceReportAsync(classId, subjectId, academicYear, ct);
            return Ok(report);
        }
    }
}
