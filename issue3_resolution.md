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
