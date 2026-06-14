# Phase 1 — Identity, Access Management & Subscription Gating

**Developer:** Dev 1  
**Complexity:** High  
**Dependencies:** None — this is the foundation all other phases build on

---

## Objective

Deliver the complete authentication, authorization, and subscription-gating vertical slice. Every user type (Student, Teacher, Parent, Admin) can register, log in, manage their session, and have access enforced by their active subscription. The hard paywall is enforced at the API middleware level — no academic feature in any other phase is reachable without a valid, active subscription resolved from this layer.

---

## 1. Functional Requirements

- User registration with role selection (Student, Teacher, Parent, Admin)
- Parent registers independently, then links to a student via a unique student linkage code
- JWT-based authentication with refresh token rotation (already implemented in Phase 2 — extend, do not rewrite)
- Role-based authorization policies: AdminOnly, TeacherOnly, StudentOnly, ParentOnly, AdminOrTeacher, StudentOrParent, AnyAuthenticated
- Subscription plans: Monthly, Per-Subject, Full-Curriculum
- Admin can manually open access for a student (cash payment path — sets subscription active without Stripe)
- Stripe integration for online subscription purchase
- Subscription status middleware: any request to a protected academic endpoint returns `402 Payment Required` if no active subscription exists
- Subscription expiry enforcement: background job checks and deactivates expired subscriptions
- Student linkage code generation and parent-student association

---

## 2. Domain Layer (`Masarak.Domain`)

### Entities

```csharp
// Already exists — extend only
public class User
{
    public int UserId { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Phone { get; private set; }
    public string Country { get; private set; }
    public int RoleId { get; private set; }
    public bool IsActive { get; private set; }
    public int? FailedLoginAttempts { get; private set; }
    public DateTime? LockoutUntil { get; private set; }
    // NEW fields:
    public string? StudentLinkageCode { get; private set; } // generated for Student role only
    public Role Role { get; private set; }
    public ICollection<RefreshToken> RefreshTokens { get; private set; }
    public ICollection<Subscription> Subscriptions { get; private set; }
}

public class Plan
{
    public int PlanId { get; private set; }
    public string Name { get; private set; }           // "Monthly", "Per-Subject", "Full-Curriculum"
    public PlanType Type { get; private set; }         // enum: Monthly, PerSubject, FullCurriculum
    public decimal Price { get; private set; }
    public string Currency { get; private set; }       // "USD", "EGP"
    public int DurationDays { get; private set; }      // 30, 90, 365
    public bool IsActive { get; private set; }
    public ICollection<Subscription> Subscriptions { get; private set; }
}

public class Subscription
{
    public int SubscriptionId { get; private set; }
    public int UserId { get; private set; }
    public int PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }  // enum: Pending, Active, Expired, Cancelled
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ActivationMethod ActivationMethod { get; private set; } // enum: Stripe, AdminManual, Cash
    public string? StripeSessionId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public string? AdminNote { get; private set; }
    public int? ActivatedByAdminId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public User User { get; private set; }
    public Plan Plan { get; private set; }
}

public class Payment
{
    public int PaymentId { get; private set; }
    public int SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }  // enum: Pending, Completed, Failed, Refunded
    public PaymentProvider Provider { get; private set; } // enum: Stripe, Manual
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeChargeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Subscription Subscription { get; private set; }
}

// For Parent-Student association
public class ParentStudentLink
{
    public int ParentStudentLinkId { get; private set; }
    public int ParentUserId { get; private set; }
    public int StudentUserId { get; private set; }
    public DateTime LinkedAt { get; private set; }
    public User Parent { get; private set; }
    public User Student { get; private set; }
}
```

### Value Objects

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);
}

public record StudentLinkageCode(string Value)
{
    public static StudentLinkageCode Generate() => 
        new(Guid.NewGuid().ToString("N")[..8].ToUpper()); // 8-char alphanumeric
}
```

### Enums (in `Masarak.Domain/Enums/`)

```csharp
public enum PlanType { Monthly, PerSubject, FullCurriculum }
public enum SubscriptionStatus { Pending, Active, Expired, Cancelled }
public enum ActivationMethod { Stripe, AdminManual, Cash }
public enum PaymentStatus { Pending, Completed, Failed, Refunded }
public enum PaymentProvider { Stripe, Manual }
```

### Domain Events

```csharp
public record SubscriptionActivatedEvent(int UserId, int SubscriptionId, DateTime EndDate);
public record SubscriptionExpiredEvent(int UserId, int SubscriptionId);
public record ParentStudentLinkedEvent(int ParentUserId, int StudentUserId);
```

---

## 3. Application Layer (`Masarak.Application`)

### Interfaces

```csharp
// Repository interfaces
public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveByUserIdAsync(int userId, CancellationToken ct);
    Task<IEnumerable<Subscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken ct);
    Task AddAsync(Subscription subscription, CancellationToken ct);
    Task UpdateAsync(Subscription subscription, CancellationToken ct);
}

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(int planId, CancellationToken ct);
    Task<IEnumerable<Plan>> GetAllActiveAsync(CancellationToken ct);
}

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct);
    Task UpdateAsync(Payment payment, CancellationToken ct);
    Task<Payment?> GetByStripeSessionIdAsync(string sessionId, CancellationToken ct);
}

public interface IParentStudentLinkRepository
{
    Task<bool> LinkExistsAsync(int parentId, int studentId, CancellationToken ct);
    Task AddAsync(ParentStudentLink link, CancellationToken ct);
    Task<IEnumerable<ParentStudentLink>> GetByParentIdAsync(int parentId, CancellationToken ct);
}

// Service interfaces
public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(int userId, int planId, string successUrl, string cancelUrl, CancellationToken ct);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature);
    Task<StripeWebhookEvent> ParseWebhookEventAsync(string payload, string signature);
}

public interface ISubscriptionAccessService
{
    Task<bool> HasActiveSubscriptionAsync(int userId, CancellationToken ct);
}
```

### Commands

```csharp
// Stripe online purchase
public record InitiateSubscriptionCheckoutCommand(int UserId, int PlanId, string SuccessUrl, string CancelUrl)
    : IRequest<InitiateSubscriptionCheckoutResult>;
public record InitiateSubscriptionCheckoutResult(string CheckoutUrl, string SessionId);

// Stripe webhook handler
public record HandleStripeWebhookCommand(string Payload, string Signature) : IRequest<Unit>;

// Admin manual activation (cash / external payment)
public record AdminActivateSubscriptionCommand(int AdminId, int StudentUserId, int PlanId, string? Note)
    : IRequest<SubscriptionDto>;

// Admin cancel subscription
public record AdminCancelSubscriptionCommand(int AdminId, int SubscriptionId, string Reason)
    : IRequest<Unit>;

// Parent links to student
public record LinkParentToStudentCommand(int ParentUserId, string StudentLinkageCode)
    : IRequest<ParentStudentLinkDto>;
```

### Queries

```csharp
public record GetActiveSubscriptionQuery(int UserId) : IRequest<SubscriptionDto?>;
public record GetAllPlansQuery() : IRequest<IEnumerable<PlanDto>>;
public record GetSubscriptionsByUserQuery(int UserId) : IRequest<IEnumerable<SubscriptionDto>>;
public record GetLinkedStudentsQuery(int ParentUserId) : IRequest<IEnumerable<LinkedStudentDto>>;
// Admin queries
public record GetAllSubscriptionsQuery(int PageNumber, int PageSize, SubscriptionStatus? Status)
    : IRequest<PagedResult<SubscriptionDto>>;
```

### DTOs

```csharp
public record SubscriptionDto(
    int SubscriptionId,
    int UserId,
    string UserFullName,
    string PlanName,
    PlanType PlanType,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    ActivationMethod ActivationMethod,
    string? AdminNote
);

public record PlanDto(int PlanId, string Name, PlanType Type, decimal Price, string Currency, int DurationDays);

public record ParentStudentLinkDto(int LinkId, int ParentUserId, int StudentUserId, string StudentFullName, DateTime LinkedAt);

public record LinkedStudentDto(int StudentUserId, string FullName, string Email, bool HasActiveSubscription);
```

### Validators

```csharp
// InitiateSubscriptionCheckoutCommandValidator
// - UserId > 0
// - PlanId > 0
// - SuccessUrl and CancelUrl are valid absolute URIs

// AdminActivateSubscriptionCommandValidator
// - StudentUserId > 0, PlanId > 0
// - Note length <= 500 chars

// LinkParentToStudentCommandValidator
// - StudentLinkageCode not null/empty, exactly 8 chars alphanumeric
```

### Command Handlers

**`InitiateSubscriptionCheckoutHandler`**
1. Validate no active subscription already exists for user
2. Load plan from `IPlanRepository`
3. Call `IStripeService.CreateCheckoutSessionAsync`
4. Create `Subscription` with `Status = Pending`, `StripeSessionId` set
5. Persist via `ISubscriptionRepository`
6. Return checkout URL

**`HandleStripeWebhookHandler`**
1. Validate signature via `IStripeService.ValidateWebhookSignatureAsync` — reject invalid signatures with `400`
2. Parse event type
3. On `checkout.session.completed`: find `Subscription` by `StripeSessionId`, set `Status = Active`, set `StartDate = now`, set `EndDate = now + plan.DurationDays`, create `Payment` record with `Status = Completed`
4. On `invoice.payment_failed`: update `Payment.Status = Failed`
5. Publish `SubscriptionActivatedEvent` via `IMediator`

**`AdminActivateSubscriptionHandler`**
1. Verify calling user has Admin role
2. Verify student exists and has Student role
3. Load plan
4. Check no existing active subscription
5. Create `Subscription` with `Status = Active`, `ActivationMethod = AdminManual` or `Cash`, set dates
6. Create `Payment` with `Provider = Manual`, `Status = Completed`
7. Publish `SubscriptionActivatedEvent`

**`LinkParentToStudentHandler`**
1. Resolve student by `StudentLinkageCode` from `IUserRepository`
2. Verify student has Student role
3. Check link does not already exist
4. Create `ParentStudentLink`
5. Publish `ParentStudentLinkedEvent`

---

## 4. Infrastructure Layer (`Masarak.Infrastructure`)

### EF Core Configuration

```csharp
// SubscriptionConfiguration.cs
builder.ToTable("subscriptions");
builder.HasKey(s => s.SubscriptionId);
builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
builder.Property(s => s.ActivationMethod).HasConversion<string>().HasMaxLength(20);
builder.Property(s => s.StripeSessionId).HasMaxLength(200);
builder.Property(s => s.StripeSubscriptionId).HasMaxLength(200);
builder.Property(s => s.AdminNote).HasMaxLength(500);
builder.HasOne(s => s.User).WithMany(u => u.Subscriptions).HasForeignKey(s => s.UserId);
builder.HasOne(s => s.Plan).WithMany(p => p.Subscriptions).HasForeignKey(s => s.PlanId);

// PlanConfiguration.cs
builder.ToTable("plans");
builder.HasKey(p => p.PlanId);
builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
builder.Property(p => p.Price).HasPrecision(10, 2);
builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

// PaymentConfiguration.cs
builder.ToTable("payments");
builder.HasKey(p => p.PaymentId);
builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(20);
builder.Property(p => p.Amount).HasPrecision(10, 2);
builder.HasOne(p => p.Subscription).WithMany().HasForeignKey(p => p.SubscriptionId);

// ParentStudentLinkConfiguration.cs
builder.ToTable("parent_student_links");
builder.HasKey(l => l.ParentStudentLinkId);
builder.HasIndex(l => new { l.ParentUserId, l.StudentUserId }).IsUnique();
builder.HasOne(l => l.Parent).WithMany().HasForeignKey(l => l.ParentUserId);
builder.HasOne(l => l.Student).WithMany().HasForeignKey(l => l.StudentUserId);
```

### Stripe Service Implementation

```csharp
// NuGet: Stripe.net
public class StripeService : IStripeService
{
    // Inject IConfiguration for Stripe:SecretKey, Stripe:WebhookSecret
    // CreateCheckoutSessionAsync: creates Stripe Checkout Session with line_items from Plan
    // ValidateWebhookSignatureAsync: EventUtility.ConstructEvent with endpointSecret
    // ParseWebhookEventAsync: returns typed StripeWebhookEvent DTO
}
```

### Background Job — Subscription Expiry

```csharp
// Uses IHostedService + Timer (or Hangfire if added later)
// Runs every 6 hours
// Calls ISubscriptionRepository.GetExpiredActiveSubscriptionsAsync
// Sets Status = Expired, publishes SubscriptionExpiredEvent
// Logs each expiry
public class SubscriptionExpiryJob : BackgroundService { ... }
```

### Subscription Access Middleware

```csharp
// SubscriptionAccessMiddleware.cs
// Applied globally after JWT authentication
// Reads HttpContext.User.GetUserId()
// Skips: auth endpoints, webhook endpoint, plan listing endpoint
// Calls ISubscriptionAccessService.HasActiveSubscriptionAsync
// Returns 402 Payment Required with structured error if no active subscription
// Caches result in IMemoryCache for 5 minutes per userId
```

### Redis Caching for Subscription Status

```csharp
// ISubscriptionAccessService implementation uses IDistributedCache (Redis)
// Cache key: subscription:active:{userId}
// TTL: 10 minutes
// Invalidated on SubscriptionActivatedEvent and SubscriptionExpiredEvent
```

### Seeder Update

```csharp
// SeedPlans.cs — seeds 3 default plans on startup if table is empty:
// Monthly: $9.99, 30 days
// Per-Subject: $4.99, 30 days  
// Full-Curriculum: $24.99, 365 days
```

---

## 5. API Layer (`Masarak.API`)

### Controllers

```
POST   /api/subscriptions/checkout          → InitiateSubscriptionCheckoutCommand
POST   /api/subscriptions/webhook           → HandleStripeWebhookCommand (no auth — webhook)
POST   /api/subscriptions/admin/activate    → AdminActivateSubscriptionCommand [AdminOnly]
POST   /api/subscriptions/admin/cancel/{id} → AdminCancelSubscriptionCommand [AdminOnly]
GET    /api/subscriptions/me                → GetActiveSubscriptionQuery [AnyAuthenticated]
GET    /api/subscriptions/me/history        → GetSubscriptionsByUserQuery [AnyAuthenticated]
GET    /api/plans                           → GetAllPlansQuery (public)
POST   /api/parent/link-student             → LinkParentToStudentCommand [ParentOnly]
GET    /api/parent/linked-students          → GetLinkedStudentsQuery [ParentOnly]
GET    /api/admin/subscriptions             → GetAllSubscriptionsQuery [AdminOnly]
```

### Auth Controller Extension (existing)

Add to existing `AuthController`:
```
GET  /api/auth/my-linkage-code  → returns StudentLinkageCode for authenticated Student
```

### Error Responses

```json
// 402 Payment Required (no active subscription)
{
  "code": "SUBSCRIPTION_REQUIRED",
  "message": "An active subscription is required to access this resource.",
  "checkoutUrl": "/subscribe"
}
```

---

## 6. Database Migration

```
Migration name: Phase1_SubscriptionAndPaymentTables

New tables: plans, subscriptions, payments, parent_student_links
Modified: users — add student_linkage_code NVARCHAR(8) NULL UNIQUE

New indexes:
- subscriptions(user_id, status) — frequent subscription lookup
- subscriptions(stripe_session_id) — webhook lookup
- parent_student_links(parent_user_id, student_user_id) UNIQUE
- users(student_linkage_code) UNIQUE FILTERED (WHERE student_linkage_code IS NOT NULL)
```

---

## 7. Angular Frontend (Dev 1 owns all of these)

### Module structure

```
src/
  app/
    features/
      auth/
        pages/
          login/
          register/
          register-parent/   ← separate parent registration page
        components/
          login-form/
          register-form/
          link-student-form/  ← parent enters linkage code
        services/
          auth.service.ts
        store/
          auth.state.ts      ← NgRx state
          auth.actions.ts
          auth.effects.ts
          auth.selectors.ts
      subscription/
        pages/
          plans/              ← public plan listing
          checkout-success/   ← Stripe redirect landing
          checkout-cancel/
          my-subscription/    ← current subscription status
        components/
          plan-card/
          subscription-status-banner/  ← shown on every page if no active sub
        services/
          subscription.service.ts
        store/
          subscription.state.ts
          subscription.actions.ts
          subscription.effects.ts
          subscription.selectors.ts
```

### Angular Models (TypeScript interfaces)

```typescript
export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest { fullName: string; email: string; password: string; confirmPassword: string; phone: string; country: string; role: 'Student' | 'Teacher' | 'Parent' | 'Admin'; }
export interface AuthResponse { success: boolean; accessToken: string; refreshToken: string; accessTokenExpiry: string; user: UserInfo; }
export interface UserInfo { userId: number; fullName: string; email: string; role: string; isActive: boolean; }
export interface Plan { planId: number; name: string; type: string; price: number; currency: string; durationDays: number; }
export interface Subscription { subscriptionId: number; planName: string; status: 'Pending' | 'Active' | 'Expired' | 'Cancelled'; startDate: string; endDate: string; activationMethod: string; }
export interface LinkStudentRequest { studentLinkageCode: string; }
```

### NgRx Auth State

```typescript
// auth.state.ts
export interface AuthState {
  user: UserInfo | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
// Selectors: selectUser, selectIsAuthenticated, selectUserRole
// Effects: login$, register$, logout$, refreshToken$
// Token stored in localStorage — refresh token in httpOnly cookie pattern via interceptor
```

### Angular Services

```typescript
// auth.service.ts
// - login(req): POST /api/auth/login
// - register(req): POST /api/auth/register
// - logout(refreshToken): POST /api/auth/logout
// - refreshToken(at, rt): POST /api/auth/refresh
// - getMyLinkageCode(): GET /api/auth/my-linkage-code

// subscription.service.ts
// - getPlans(): GET /api/plans
// - initiateCheckout(planId): POST /api/subscriptions/checkout
// - getMySubscription(): GET /api/subscriptions/me

// JWT interceptor: attaches Bearer token to all requests
// Refresh interceptor: on 401, attempts token refresh, retries once, logs out on second failure
```

### Angular Routing

```typescript
// app-routing.module.ts additions
{ path: 'login', component: LoginPage },
{ path: 'register', component: RegisterPage },
{ path: 'plans', component: PlansPage },
{ path: 'checkout/success', component: CheckoutSuccessPage },
{ path: 'checkout/cancel', component: CheckoutCancelPage },
{ path: 'my-subscription', component: MySubscriptionPage, canActivate: [AuthGuard] },
// AuthGuard: checks selectIsAuthenticated, redirects to /login
// SubscriptionGuard: checks active subscription, redirects to /plans
```

---

## 8. Integration & Testing Tasks

### Unit Tests (xUnit + Moq)

- `InitiateSubscriptionCheckoutHandler`: verify plan loads, Stripe called, Subscription created Pending
- `HandleStripeWebhookHandler`: verify `checkout.session.completed` activates subscription and creates Payment
- `AdminActivateSubscriptionHandler`: verify admin-only enforcement, subscription created Active
- `LinkParentToStudentHandler`: verify linkage code lookup, duplicate prevention
- `SubscriptionAccessMiddleware`: verify 402 on missing subscription, skip for auth routes

### Integration Tests (WebApplicationFactory)

- Full register → initiate checkout → simulate Stripe webhook → verify subscription active → access protected endpoint returns 200
- Admin manual activation flow → access protected endpoint returns 200
- Parent registration → link student by code → verify linked students query returns student
- Expired subscription → background job run → verify 402 returned

### Angular Tests (Jasmine/Karma or Jest)

- `AuthService`: mock HttpClient, verify login stores token
- `AuthGuard`: verify redirect on unauthenticated
- `SubscriptionGuard`: verify redirect to /plans on no active subscription
- `SubscriptionStatusBanner`: shown when no active subscription

---

## 9. Definition of Done

- [ ] All domain entities compile with private setters and factory methods
- [ ] Stripe webhook endpoint validates signatures and handles `checkout.session.completed`
- [ ] Admin can activate subscription manually (cash path)
- [ ] Parent can register and link to student via linkage code
- [ ] `SubscriptionAccessMiddleware` returns `402` for all academic routes with no active subscription
- [ ] Redis caches subscription status with proper invalidation
- [ ] Background job expires overdue subscriptions every 6 hours
- [ ] Angular plan listing page renders, user can click a plan and reach Stripe checkout
- [ ] Angular auth store persists token, interceptor attaches it, refresh flow works end-to-end
- [ ] All unit and integration tests pass
- [ ] EF migration applies cleanly on a fresh database
- [ ] Stripe webhook tested with Stripe CLI `stripe listen --forward-to`

---

## 10. Risks

| Risk | Mitigation |
|---|---|
| Stripe webhook delivery order not guaranteed | Implement idempotency check on StripeSessionId before activating |
| Redis unavailable in dev | Fallback to IMemoryCache via feature flag |
| Subscription middleware blocks other devs during integration | Provide a dev-only bypass header `X-Dev-Bypass-Subscription: true` guarded by `IsDevelopment()` |
| Parent linkage code collision | Use 8-char base32; regenerate on collision with max 3 retries |
