# Issue Resolution Report: Admin Subscription Activation 500 Error

## 1. The Problem
The frontend team reported that calling the endpoint `POST /api/subscriptions/admin/activate` resulted in a **500 Internal Server Error**. 

This endpoint is used by administrators to manually activate a subscription for a student (e.g., when a student pays in cash). When the frontend sent a request, instead of succeeding or giving a clear error message, the server crashed and returned a generic 500 error.

## 2. The Reason (Root Cause)
The crash was caused by a **missing validation check resulting in a database-level Foreign Key constraint violation**.

Specifically, inside the `AdminActivateAsync` method in `SubscriptionService.cs`:
1. The method checked if the `PlanId` existed.
2. It checked if the student already had an active subscription.
3. However, **it never verified if the `StudentUserId` actually belonged to a real user in the database.**

When the frontend sent an invalid or non-existent `StudentUserId`, the code attempted to save the new `Subscription` record to the database. SQL Server immediately rejected the insert because of the Foreign Key constraint linking `subscriptions.UserId` to `users.UserId`. 

This database rejection caused Entity Framework to throw a `DbUpdateException`. Because this specific exception was not handled by the controller, it bubbled all the way up as an unhandled crash, returning the `500 Internal Server Error`.

## 3. What We Made to Solve It
To fix the issue, we added proactive, defensive validation before any database records are created. 

We modified `Masarak.Infrastructure/Services/SubscriptionService.cs` (inside `AdminActivateAsync`) to explicitly fetch and validate the user:

```csharp
// 1. Validate that the target user exists
var studentUser = await _userRepository.GetByIdAsync(request.StudentUserId, ct);
if (studentUser == null) 
    throw new InvalidOperationException("User not found.");

// 2. Validate that the user is actually a Student (not a Teacher or Parent)
if (studentUser.Role.Name != "Student") 
    throw new InvalidOperationException("Subscription can only be activated for Student users.");
```

**Why this works:**
By checking if the user exists *before* trying to create the subscription, we catch the mistake early. If the user doesn't exist, we throw an `InvalidOperationException`. 

The `SubscriptionController` is already designed to catch `InvalidOperationException` and translate it into a clean `400 Bad Request`.

**Result:** 
Instead of a database crash and a 500 error, the API now safely rejects the invalid request and returns a helpful 400 error to the frontend:
`{"message": "User not found."}`

# Issue 4 Resolution: No Data Returns From Database in GET Methods

## 1. The Problem
The frontend team reported that all `GET` API endpoints were returning empty arrays or no data. As a result, the admin and teacher dashboards in the Angular frontend appeared entirely blank (e.g., no teachers, no students, no classes, no subjects).

## 2. The Reason
After auditing the backend, we found that this was **not a code bug** in the `GET` methods themselves. The methods were working perfectly, but the database was simply empty. 

The `DatabaseSeeder.cs` class was only designed to seed **minimum bootstrap data** needed for the application to function (like the Admin user, 4 Roles, 12 Grades, and Subscription Plans). It did not seed any actual test data for everyday platform usage. Because the system had no teachers, students, subjects, or classes in the database, the frontend had nothing to display.

## 3. What We Made to Solve It
To allow the frontend team to test their UI and verify the application's functionality without having to manually create records every time, we implemented **Development Data Seeders**. 

### Changes Made:
1. **Added 6 New Seed Methods to `DatabaseSeeder.cs`**:
   - `SeedTestTeachersAsync`: Created 2 test teachers (Ahmed & Mona) with specializations.
   - `SeedTestStudentsAsync`: Created 2 test students (Youssef & Salma) with active academic statuses.
   - `SeedSubjectsAsync`: Created 3 subjects for Grade 1 (Mathematics, Science, Arabic).
   - `SeedClassesAsync`: Created 2 classes (1A, 1B).
   - `SeedTeachingAssignmentsAsync`: Assigned a teacher to a class and subject.
   - `SeedStudentEnrollmentsAsync`: Enrolled a student into a class.

2. **Updated `Program.cs`**:
   - We updated the `Program.cs` startup pipeline to execute these new seeders automatically whenever the application boots up.
   - **Important Security Measure:** We wrapped these new seeders inside an `if (app.Environment.IsDevelopment())` block. This guarantees that test data will **only** be generated when running locally in development, preventing fake data from accidentally being inserted into a live production database.

### Result
The backend API has been restarted and the seeders successfully executed. When the Angular frontend calls the `GET` endpoints, it will now receive populated data arrays, allowing developers to see and interact with the UI correctly.

# Issue 5 Resolution: Subscription Activation 400 Bad Request & Missing User Endpoints

## 1. The Problem
The frontend team reported a `400 Bad Request` when clicking "Assign Subscription" in the Admin User Management dashboard. The UI displayed a generic error message, and the user was completely unable to successfully assign a subscription to a student.

## 2. The Reason
There were two intertwined issues causing this frustrating experience:

1. **Frontend Error Masking:** The Angular frontend was receiving a valid validation error from the backend (e.g., "Student already has an active subscription" or "User not found"), but the frontend code was ignoring the message. It swallowed the backend error and displayed a hardcoded generic message, leaving the user confused about *why* the 400 occurred.
2. **Missing Backend Endpoints (The Root Block):** The admin dropdown for selecting a student was empty or only contained one student because the backend endpoints `GET /api/admin/users` and `POST /api/admin/users` were completely missing from the project. This forced the admin to either select the only available user (who *already* had a subscription, correctly triggering a 400 rejection) or try to use locally mocked "manual" users (which correctly triggered a "User not found" 400 rejection).

## 3. What We Made to Solve It
To permanently unblock the frontend and make the flow work, we fixed both the frontend error handling and the missing backend architecture:

### Frontend Changes:
- Updated `user-management.ts` to extract `err?.error?.message` from the API response. The UI now correctly displays the exact backend validation error on the screen instead of a generic message.
- Added validation to prevent the frontend from attempting to assign subscriptions to fake "manual" users that only exist in `localStorage`.

### Backend Changes:
We implemented the missing Admin endpoints so the frontend can fetch and create real students:
1. **Created Service Layer:** Added `IAdminUserService.cs` and `AdminUserService.cs` to handle fetching, creating, and deleting users from the database.
2. **Updated Controllers:** 
   - Implemented `GET /api/admin/users`, `POST /api/admin/users`, and `DELETE /api/admin/users/{id}` in `AdminController.cs`.
   - Implemented `GET /api/admin/teachers` in `AcademicAdminController.cs`.
3. **Dependency Injection:** Registered the new service in `ServiceCollectionExtensions.cs`.

### Result
The frontend now successfully fetches real students from the database. Admins can click "Add User" to create brand new students, and successfully assign subscriptions to any student who does not already have one. If a business rule is violated, the exact reason is now beautifully displayed in the UI.

# Issue 6 Resolution: Subscription Cancellation 400 Bad Request

## 1. The Problem
The frontend team reported another `400 Bad Request` when an administrator clicked the "Cancel Subscription" button for a user. The network request failed at `POST /api/subscriptions/admin/cancel/0`.

## 2. The Reason
The URL clearly showed that the frontend was trying to cancel a subscription with an ID of `0`. Because `0` is an invalid auto-increment ID, the backend correctly queried the database, found no matching subscription, and threw an `InvalidOperationException("Subscription not found.")`, which resulted in the 400 error.

The root cause of this was in the frontend code (`user-management.html`). The frontend developer had hardcoded `0` into the template:
```html
(click)="cancelSubscription(user.id, 0)"
```
This hardcoding occurred because the `AdminUser` TypeScript interface was entirely missing the `subscriptionId` property. Because the interface didn't expose the real ID from the backend, the developer used `0` as a placeholder to get the code to compile, causing the actual API call to fail.

## 3. What We Made to Solve It
To fix this, we updated the frontend to capture and pass the correct subscription ID.

### Changes Made:
1. **Updated TypeScript Interface:** Added `subscriptionId: number;` to the `subscription` object inside the `AdminUser` interface in `user-management.ts`.
2. **Mapped the Data:** Updated the `mapSub` mapping function and the `assignSubscription` state updater to correctly capture the `subscriptionId` from the backend `SubscriptionDto` response.
3. **Fixed the HTML Template:** Updated the button in `user-management.html` to pass the real dynamic ID instead of the hardcoded `0`:
   ```html
   (click)="cancelSubscription(user.id, user.subscription.subscriptionId)"
   ```

### Result
The "Cancel Subscription" button now correctly sends the valid subscription ID to the backend (e.g., `/api/subscriptions/admin/cancel/2`). The backend successfully finds the subscription, cancels it, and the UI immediately updates without any 400 errors.
