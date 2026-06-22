using System.Security.Claims;
using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/teacher/assessment")]
    [Authorize(Policy = AppPolicies.TeacherOnly)]
    [Produces("application/json")]
    public class TeacherAssessmentController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;

        public TeacherAssessmentController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        // ── Assignments ────────────────────────────────────────────────────────

        [HttpPost("assignments")]
        [ProducesResponseType(typeof(AssignmentDto), 201)]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request, CancellationToken ct)
        {
            var dto = await _assessmentService.CreateAssignmentAsync(GetUserId(), request, ct);
            return Created("", dto);
        }

        [HttpPost("assignments/{id}/publish")]
        public async Task<IActionResult> PublishAssignment(int id, CancellationToken ct)
        {
            await _assessmentService.PublishAssignmentAsync(GetUserId(), id, ct);
            return NoContent();
        }

        [HttpPost("assignments/{id}/close")]
        public async Task<IActionResult> CloseAssignment(int id, CancellationToken ct)
        {
            await _assessmentService.CloseAssignmentAsync(GetUserId(), id, ct);
            return NoContent();
        }

        [HttpGet("teaching-assignments/{taId}/assignments")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), 200)]
        public async Task<IActionResult> GetAssignments(int taId, CancellationToken ct)
        {
            var dtos = await _assessmentService.GetTeacherAssignmentsAsync(GetUserId(), taId, ct);
            return Ok(dtos);
        }

        // ── Submissions ────────────────────────────────────────────────────────

        [HttpGet("assignments/{assignmentId}/submissions")]
        [ProducesResponseType(typeof(IEnumerable<SubmissionDetailDto>), 200)]
        public async Task<IActionResult> GetSubmissions(int assignmentId, CancellationToken ct)
        {
            var dtos = await _assessmentService.GetSubmissionsForAssignmentAsync(GetUserId(), assignmentId, ct);
            return Ok(dtos);
        }

        [HttpPost("submissions/{submissionId}/grade")]
        [ProducesResponseType(typeof(SubmissionDto), 200)]
        public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionRequest request, CancellationToken ct)
        {
            var dto = await _assessmentService.GradeSubmissionAsync(GetUserId(), submissionId, request, ct);
            return Ok(dto);
        }

        [HttpGet("submissions/{submissionId}/file")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> GetSubmissionFile(int submissionId, CancellationToken ct)
        {
            var url = await _assessmentService.GetSubmissionFileDownloadUrlAsync(GetUserId(), submissionId, ct);
            return Ok(new { url });
        }

        // ── Exams ──────────────────────────────────────────────────────────────

        [HttpPost("exams")]
        [ProducesResponseType(typeof(ExamDto), 201)]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request, CancellationToken ct)
        {
            var dto = await _assessmentService.CreateExamAsync(GetUserId(), request, ct);
            return Created("", dto);
        }

        [HttpPost("exams/{id}/publish")]
        public async Task<IActionResult> PublishExam(int id, CancellationToken ct)
        {
            await _assessmentService.PublishExamAsync(GetUserId(), id, ct);
            return NoContent();
        }

        [HttpGet("teaching-assignments/{taId}/exams")]
        [ProducesResponseType(typeof(IEnumerable<ExamDto>), 200)]
        public async Task<IActionResult> GetExams(int taId, CancellationToken ct)
        {
            var dtos = await _assessmentService.GetTeacherExamsAsync(GetUserId(), taId, ct);
            return Ok(dtos);
        }

        // ── Questions ──────────────────────────────────────────────────────────

        [HttpPost("exams/{examId}/questions")]
        [ProducesResponseType(typeof(QuestionDto), 201)]
        public async Task<IActionResult> AddQuestion(int examId, [FromBody] AddQuestionRequest request, CancellationToken ct)
        {
            var dto = await _assessmentService.AddQuestionToExamAsync(GetUserId(), examId, request, ct);
            return Created("", dto);
        }
    }
}
