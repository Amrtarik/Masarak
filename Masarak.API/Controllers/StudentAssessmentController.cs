using System.Security.Claims;
using Masarak.API.Policies;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    [ApiController]
    [Route("api/student/assessment")]
    [Authorize(Policy = AppPolicies.StudentOnly)]
    [Produces("application/json")]
    public class StudentAssessmentController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;

        public StudentAssessmentController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        // ── Assignments ────────────────────────────────────────────────────────

        [HttpGet("subjects/{subjectId}/assignments")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), 200)]
        public async Task<IActionResult> GetAssignments(int subjectId, CancellationToken ct)
        {
            var dtos = await _assessmentService.GetStudentAssignmentsAsync(GetUserId(), subjectId, ct);
            return Ok(dtos);
        }

        [HttpPost("assignments/{assignmentId}/submit")]
        [ProducesResponseType(typeof(SubmissionDto), 201)]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromForm] SubmitAssignmentRequest request, IFormFile? file, CancellationToken ct)
        {
            Stream? fileStream = null;
            string? fileName = null;
            string? contentType = null;

            if (file != null)
            {
                fileStream = file.OpenReadStream();
                fileName = file.FileName;
                contentType = file.ContentType;
            }

            var dto = await _assessmentService.SubmitAssignmentAsync(GetUserId(), assignmentId, request.TextContent, fileStream, fileName, contentType, ct);
            return Created("", dto);
        }

        // ── Exams ──────────────────────────────────────────────────────────────

        [HttpGet("subjects/{subjectId}/exams")]
        [ProducesResponseType(typeof(IEnumerable<ExamDto>), 200)]
        public async Task<IActionResult> GetOpenExams(int subjectId, CancellationToken ct)
        {
            var dtos = await _assessmentService.GetOpenExamsForStudentAsync(GetUserId(), subjectId, ct);
            return Ok(dtos);
        }

        [HttpPost("exams/{examId}/start")]
        [ProducesResponseType(typeof(ExamAttemptDto), 200)]
        public async Task<IActionResult> StartExam(int examId, CancellationToken ct)
        {
            var dto = await _assessmentService.StartExamAttemptAsync(GetUserId(), examId, ct);
            return Ok(dto);
        }

        [HttpPost("student-exams/{studentExamId}/answers")]
        public async Task<IActionResult> SaveAnswer(int studentExamId, [FromForm] SaveAnswerRequest request, IFormFile? file, CancellationToken ct)
        {
            Stream? fileStream = null;
            string? fileName = null;
            string? contentType = null;

            if (file != null)
            {
                fileStream = file.OpenReadStream();
                fileName = file.FileName;
                contentType = file.ContentType;
            }

            await _assessmentService.SaveAnswerAsync(GetUserId(), studentExamId, request, fileStream, fileName, contentType, ct);
            return Ok();
        }

        [HttpPost("student-exams/{studentExamId}/submit")]
        [ProducesResponseType(typeof(ExamResultDto), 200)]
        public async Task<IActionResult> SubmitExam(int studentExamId, CancellationToken ct)
        {
            var dto = await _assessmentService.SubmitExamAsync(GetUserId(), studentExamId, ct);
            return Ok(dto);
        }

        [HttpGet("student-exams/{studentExamId}/result")]
        [ProducesResponseType(typeof(ExamResultDto), 200)]
        public async Task<IActionResult> GetExamResult(int studentExamId, CancellationToken ct)
        {
            var dto = await _assessmentService.GetStudentExamResultAsync(GetUserId(), studentExamId, ct);
            return Ok(dto);
        }
    }
}
