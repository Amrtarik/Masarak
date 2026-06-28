using System.Security.Claims;
using Masarak.API.Policies;
using Masarak.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Masarak.API.Controllers
{
    // ─── Admin Controller ─────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        [HttpGet("dashboard")]
        public IActionResult Dashboard() =>
            Ok(new { message = "Admin dashboard — restricted to Admins only.", user = GetUserInfo() });

        [HttpGet("users")]
        [ProducesResponseType(typeof(Masarak.Application.DTOs.PagedResult<Masarak.Application.DTOs.AdminUserDto>), 200)]
        public async Task<IActionResult> GetAllUsers(
            [FromServices] IAdminUserService adminUserService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? role = null)
        {
            var result = await adminUserService.GetUsersAsync(page, pageSize, role);
            return Ok(result);
        }

        [HttpPost("users")]
        [ProducesResponseType(typeof(Masarak.Application.DTOs.AdminUserDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateUser(
            [FromServices] IAdminUserService adminUserService,
            [FromBody] Masarak.Application.DTOs.AdminCreateUserRequest request)
        {
            try
            {
                var result = await adminUserService.CreateUserAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(
            [FromServices] IAdminUserService adminUserService,
            int id)
        {
            try
            {
                await adminUserService.DeleteUserAsync(id);
                return Ok(new { message = $"User {id} deleted by Admin." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(Masarak.Application.DTOs.PagedResult<Masarak.Application.DTOs.SubscriptionDto>), 200)]
        public async Task<IActionResult> GetAllSubscriptions(
            [FromServices] ISubscriptionService subscriptionService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Masarak.Domain.Enums.SubscriptionStatus? status = null)
        {
            var result = await subscriptionService.GetAllSubscriptionsAsync(page, pageSize, status);
            return Ok(result);
        }

        private object GetUserInfo() => new
        {
            UserId = User.FindFirstValue("userid"),
            Email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            Role = User.FindFirstValue("role")
        };
    }

    // ─── Teacher Controller (Phase 1 placeholders — academic endpoints moved to SessionTeacherController) ──
    [ApiController]
    [Route("api/teacher")]
    [Authorize(Policy = AppPolicies.TeacherOnly)]
    [Produces("application/json")]
    public class TeacherController : ControllerBase
    {
        // Admins and Teachers can both grade
        [HttpPost("grade/{submissionId}")]
        [Authorize(Policy = AppPolicies.AdminOrTeacher)]
        public IActionResult GradeSubmission(int submissionId, [FromBody] decimal score) =>
            Ok(new { message = $"Submission {submissionId} graded with {score}.", user = GetUserInfo() });

        private object GetUserInfo() => new
        {
            UserId = User.FindFirstValue("userid"),
            Email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            Role = User.FindFirstValue("role")
        };
    }

    // ─── Student Controller ───────────────────────────────────────────────────────
    [ApiController]
    [Route("api/student")]
    [Authorize(Policy = AppPolicies.StudentOnly)]
    [Produces("application/json")]
    public class StudentController : ControllerBase
    {
        [HttpGet("courses")]
        public IActionResult GetMyCourses() =>
            Ok(new { message = "My enrolled courses — Student only.", user = GetUserInfo() });

        [HttpGet("grades")]
        public IActionResult GetMyGrades() =>
            Ok(new { message = "My grades — Student only.", user = GetUserInfo() });

        [HttpPost("assignments/{id}/submit")]
        public IActionResult SubmitAssignment(int id) =>
            Ok(new { message = $"Assignment {id} submitted — Student only.", user = GetUserInfo() });

        [HttpGet("exams")]
        public IActionResult GetUpcomingExams() =>
            Ok(new { message = "Upcoming exams — Student only.", user = GetUserInfo() });

        private object GetUserInfo() => new
        {
            UserId = User.FindFirstValue("userid"),
            Email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            Role = User.FindFirstValue("role")
        };
    }

    // ─── Parent Controller ────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/parent")]
    [Authorize(Policy = AppPolicies.ParentOnly)]
    [Produces("application/json")]
    public class ParentController : ControllerBase
    {
        [HttpGet("children")]
        public IActionResult GetChildren() =>
            Ok(new { message = "My children — Parent only.", user = GetUserInfo() });

        [HttpGet("children/{childId}/grades")]
        public IActionResult GetChildGrades(int childId) =>
            Ok(new { message = $"Child {childId} grades — Parent only.", user = GetUserInfo() });

        [HttpGet("children/{childId}/attendance")]
        public IActionResult GetChildAttendance(int childId) =>
            Ok(new { message = $"Child {childId} attendance — Parent only.", user = GetUserInfo() });

        [HttpPost("link-student")]
        [ProducesResponseType(typeof(Masarak.Application.DTOs.ParentStudentLinkDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> LinkStudent([FromServices] ISubscriptionService subscriptionService, [FromBody] Masarak.Application.DTOs.LinkStudentRequest request)
        {
            var parentId = int.Parse(User.FindFirstValue("userid") ?? "0");
            if (parentId == 0) return Unauthorized();

            try
            {
                var result = await subscriptionService.LinkParentToStudentAsync(parentId, request.StudentLinkageCode);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("linked-students")]
        [ProducesResponseType(typeof(IEnumerable<Masarak.Application.DTOs.LinkedStudentDto>), 200)]
        public async Task<IActionResult> GetLinkedStudents([FromServices] ISubscriptionService subscriptionService)
        {
            var parentId = int.Parse(User.FindFirstValue("userid") ?? "0");
            if (parentId == 0) return Unauthorized();

            var students = await subscriptionService.GetLinkedStudentsAsync(parentId);
            return Ok(students);
        }

        private object GetUserInfo() => new
        {
            UserId = User.FindFirstValue("userid"),
            Email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            Role = User.FindFirstValue("role")
        };
    }

    // ─── Mixed Access Controller ──────────────────────────────────────────────────
    [ApiController]
    [Route("api/shared")]
    [Authorize]
    [Produces("application/json")]
    public class SharedController : ControllerBase
    {
        // Students and Parents can both view a student's progress
        [HttpGet("progress/{studentId}")]
        [Authorize(Policy = AppPolicies.StudentOrParent)]
        public IActionResult GetProgress(int studentId) =>
            Ok(new { message = $"Progress for student {studentId} — Student or Parent.", user = GetUserInfo() });

        // Admins and Teachers can view analytics
        [HttpGet("analytics")]
        [Authorize(Policy = AppPolicies.AdminOrTeacher)]
        public IActionResult GetAnalytics() =>
            Ok(new { message = "Analytics dashboard — Admin or Teacher.", user = GetUserInfo() });

        // Any authenticated user can get their notifications
        [HttpGet("notifications")]
        public IActionResult GetNotifications() =>
            Ok(new { message = "Notifications — any authenticated user.", user = GetUserInfo() });

        private object GetUserInfo() => new
        {
            UserId = User.FindFirstValue("userid"),
            Email = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            Role = User.FindFirstValue("role")
        };
    }
}
