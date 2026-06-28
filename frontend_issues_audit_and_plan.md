# Frontend Issues Audit and Implementation Plan

This document contains the architectural audit of the 4 issues reported by the frontend team, followed by the implementation plan to resolve the missing endpoints.

---

## Part 1: Architecture Audit of Reported Issues

### Issue 1
**Issue**
`GET /api/admin/teachers` returns a `404 Not Found` error.

**Root Cause**
Missing endpoint / Architecture misuse
The API endpoint is completely missing from the application.

**Why It Happens**
The frontend is requesting a list of teachers, but neither `AcademicAdminController.cs` nor `AdminController.cs` contains an `[HttpGet("teachers")]` action method. Since the ASP.NET router cannot find a matching route for the URL, it automatically returns a standard 404 status.

**Impact**
Frontend behavior. The admin dashboard cannot display the list of teachers, blocking the frontend team from testing teacher management features.

**Recommended Fix**
Add an `[HttpGet("teachers")]` endpoint to the appropriate Admin Controller. Create an `IAdminUserService` interface and implement the retrieval logic in a new `AdminUserService` class to fetch all `Teachers` (including their base `User` details) from the database and map them to a `TeacherDto`.

**Risk Level**
Low
Adding a new query endpoint to fetch data is a purely additive change that will not alter or break any existing features.

**Files Likely Affected**
`Masarak.API/Controllers/AcademicAdminController.cs`
`Masarak.Application/Interfaces/IAdminUserService.cs` (New)
`Masarak.Infrastructure/Services/AdminUserService.cs` (New)
`Masarak.Application/DTOs/AdminDTOs.cs`

**Confidence**
High

---

### Issue 2
**Issue**
`POST /api/auth/login` returns a `401 Unauthorized` error.

**Root Cause**
Authentication failure / Validation problem
The credentials provided by the frontend are incorrect.

**Why It Happens**
The `AuthService.LoginAsync` method verifies the user's password hash against the database. If the user does not exist or the password does not match, it returns an unsuccessful `AuthResponse`. The `AuthController.Login` method explicitly maps this failure to `return Unauthorized(result);`, sending the HTTP 401 status.

**Impact**
Security / User experience.
The user is prevented from logging in. This is the expected and correct security behavior for invalid credentials.

**Recommended Fix**
No backend fix is required. The frontend should capture the 401 status and display an "Invalid email or password" message to the user. Ensure the frontend team is using the correct test credentials that we just seeded (e.g., `ahmed.teacher@masarak.com` with `Teacher@123!`).

**Risk Level**
Low
This is functioning exactly as intended by the established architecture.

**Files Likely Affected**
None.

**Confidence**
High

---

### Issue 3
**Issue**
`GET /api/student/my-class?academicYear=2026` returns a `402 Payment Required` error.

**Root Cause**
Business Rule Enforcement / Logic problem (from the frontend's perspective)
The student making the request does not have an active subscription.

**Why It Happens**
The `SubscriptionAccessMiddleware` intercepts all requests made to student-specific educational endpoints. At line 93 of `SubscriptionAccessMiddleware.cs`, it evaluates the student's active subscription status. If the student has no subscription, or if it is expired, the middleware halts the pipeline and explicitly sets `context.Response.StatusCode = 402;` (Payment Required).

**Impact**
Business flow. Unsubscribed students are correctly prevented from viewing restricted class content, enforcing the core monetization rules of the platform.

**Recommended Fix**
No backend fix is required. The frontend must be updated to catch the 402 HTTP status code and redirect the student to the "Subscription Plans" page to renew or activate their account. 

**Risk Level**
Low
The backend is correctly enforcing the required business logic. 

**Files Likely Affected**
None.

**Confidence**
High

---

### Issue 4
**Issue**
`POST /api/subscriptions/admin/activate` returns a `400 Bad Request` error.

**Root Cause**
Validation problem
The frontend attempted to activate a subscription for a non-existent student or provided an invalid user ID.

**Why It Happens**
During the resolution of Issue 3, we added a strict validation rule to `SubscriptionService.AdminActivateAsync`. If the `studentId` passed in the request does not map to a valid `Student` record, it throws an `InvalidOperationException("User not found.")`. The `SubscriptionController` catches this exception and returns a `BadRequest` (400) containing the error message.

**Impact**
Data consistency. The system is protecting itself from creating orphaned subscriptions or accidentally activating subscriptions for teachers/admins.

**Recommended Fix**
No backend fix is required. The validation rule we added earlier successfully caught a bad payload from the frontend. The frontend team needs to ensure they are passing a valid `StudentId` in their JSON payload.

**Risk Level**
Low
Working precisely as intended to protect data integrity.

**Files Likely Affected**
None.

**Confidence**
High

---

## Part 2: Implementation Plan for Missing Endpoints (Issue 1 & 2)

As identified in the audit above, Issue 1 requires adding missing endpoints to the API. Three of the four issues reported by the frontend are intended backend behavior, meaning no backend fixes are required for those. However, the `404 Not Found` for teachers means we must write the missing service code.

### Proposed Changes

We will implement the endpoints for fetching teachers and fetching/editing users by adding them to the existing `AdminController` and `AcademicAdminController`. We will also create a new `AdminUserService` to handle the business logic cleanly without breaking existing architecture.

### Application Layer (Interfaces & DTOs)

#### [NEW] `Masarak.Application/Interfaces/IAdminUserService.cs`
- Create `IAdminUserService` interface with methods:
  - `Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(CancellationToken ct);`
  - `Task<PagedResult<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role, CancellationToken ct);`
  - `Task<AdminUserDto> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct);`
  - `Task DeleteUserAsync(int userId, CancellationToken ct);`

#### [NEW] `Masarak.Application/DTOs/AdminDTOs.cs`
- Add `AdminUserDto` to represent user data sent to the admin dashboard.
- Add `AdminCreateUserRequest` for creating new users.
- Add `TeacherDto` for the teacher directory.

### Infrastructure Layer (Services)

#### [NEW] `Masarak.Infrastructure/Services/AdminUserService.cs`
- Implement `IAdminUserService`.
- Inject `Context` directly (following the established pattern in `AuthService.cs` and `AcademicService.cs`).
- `GetAllTeachersAsync`: Query `_db.Teachers.Include(t => t.User).Select(...)`.
- `GetUsersAsync`: Query `_db.Users.Include(u => u.Role)` with pagination and role filtering.
- `DeleteUserAsync`: Soft-delete the user (`IsActive = false`) and save changes.

### API Layer (Controllers & Registration)

#### [MODIFY] `Masarak.API/Controllers/SecuredControllers.cs`
- Replace the dummy `[HttpGet("users")]` and `[HttpDelete("users/{id}")]` methods in `AdminController` with actual calls to `IAdminUserService`.
- Add `[HttpPost("users")]` to handle user creation.

#### [MODIFY] `Masarak.API/Controllers/AcademicAdminController.cs`
- Add `[HttpGet("teachers")]` which calls `IAdminUserService.GetAllTeachersAsync()`.

#### [MODIFY] `Masarak.API/Extensions/ServiceCollectionExtensions.cs`
- Register the new `AdminUserService` in the DI container.
