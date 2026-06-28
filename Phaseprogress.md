# Masarak Project - Phase Progress & Architecture Overview

## 1. Current Architecture
The project follows a **Clean Architecture** approach implemented in .NET 9, divided into three main layers:
- **Core (Domain & Application):** Contains business entities, interfaces, constants (e.g., AppRoles), and DTOs. Completely independent of infrastructure.
- **Infrastructure:** Implements Data Access using Entity Framework Core (EF Core), database context (`Context.cs`), migrations, seeders, and external services (e.g., `AuthService`, `JwtService`, `PasswordService`).
- **Presentation:** The ASP.NET Core API layer containing controllers, routing, authorization policies, middleware, and dependency injection configurations.

## 2. Bounded Contexts & Business Modules
The system is logically divided into the following bounded contexts/modules:
- **Identity & Access Management:** User registration, JWT-based authentication, refresh token rotation, role profiles, and security lockouts.
- **Academic Core:** Management of Grades, Classes, Subjects, and Teaching Assignments.
- **Assessment & Grading:** Handling Assignments, Submissions, Exams, Questions, and Student Performance tracking.
- **Attendance Management:** Tracking student session attendance with automatic present/absent recording, teacher overrides, and attendance percentage computation.
- **Content Library:** Teacher upload of videos (YouTube/Vimeo URLs or Azure Blob), PDFs, Notes, and Exercise Sheets; student browsing filtered by enrolled class and subject.
- **Community Chat:** SignalR-based real-time chat with grade community rooms and teacher community rooms; persisted message history with soft delete.
- **Financial & Subscriptions:** Managing Plans, Subscriptions, and Payments.
- **Communications & Analytics:** Notifications, Dashboards, AI Recommendations, and system-wide analytics.

## 3. Existing Database Design
The database utilizes SQL Server via EF Core and consists of 30 core entities with strict relational integrity:
- **Identity:** `users`, `roles`, `refresh_tokens`
- **Profiles:** `students`, `teachers`, `parents`, `parent_student`
- **Academic:** `grades`, `classes`, `student_class`, `subjects`, `teaching_assignments`, `sessions`
- **Assessment:** `assignments`, `submissions`, `exams`, `questions`, `student_exams`, `student_answers`, `student_performance`
- **Financial:** `plans`, `subscriptions`, `payments`
- **Phase 4 — Attendance/Content/Chat:** `attendance` (updated), `content_items`, `chat_rooms`, `chat_messages`
- **Auxiliary:** `ai_recommendations`, `notifications`

## 4. User Roles & Permissions
The system employs a robust Role-Based Access Control (RBAC) mechanism using JWT claims:
- **Roles:** `Admin`, `Teacher`, `Student`, `Parent`.
- **Authorization Policies:**
  - `AdminOnly`: Full CRUD over system resources, user management, and global financial views.
  - `TeacherOnly`: Manage assigned classes, create sessions, post assignments, author exams, grade submissions, upload content, override attendance.
  - `StudentOnly`: View enrolled courses, submit assignments, take exams, view own attendance/grades, browse content library.
  - `ParentOnly`: View linked children's progress, grades, and attendance.
  - Shared Policies: `AdminOrTeacher`, `StudentOrParent`, `AnyAuthenticated`.

## 5. External Integrations
- **Database:** Microsoft SQL Server (via EF Core).
- **Security:** Custom PBKDF2-SHA512 password hashing, JWT Bearer authentication.
- **Real-time:** SignalR with optional Redis backplane for multi-instance deployments.
- **Storage:** Local file storage (dev) / Azure Blob Storage (prod) for content and submission files.
- **Testing:** Postman / Newman for automated API Integration and Security Tests.
- **Planned/Implicit:** AI Integration for `AiRecommendation`, Payment Gateway for `Payment` processing.

## 6. Current Progress
### ✅ Completed Phases
- **Phase 1: Initial Architecture & Database Design** - Scaffolded the 26 entities, relationships, and Clean Architecture structure.
- **Phase 2: Authentication & Student Management** - Custom Identity implementation, JWT integration, Refresh Tokens, and base Student CRUD.
- **System Hardening & Security Testing** - Executed forensic architectural audits, resolved referential integrity issues, automated security testing via Newman, and verified SOLID compliance.
- **Architecture Migration Refactor** - Successfully refactored legacy components into strict Clean Architecture layers ensuring 100% functional parity.

### ✅ Phase 4: Attendance, Content Library & Chat (Completed 2026-06-25)

#### Domain Layer
- **Entities:** Updated `Attendance` (enum status, factory methods, private setters, StudentUserId→User FK, TeacherNote, RecordedAt); Created `ContentItem`, `ChatRoom`, `ChatMessage`
- **Enums:** `AttendanceStatus`, `ContentType`, `ContentSourceType`, `ChatRoomType`
- **Domain Services:** `AttendanceWindowChecker` (±15min join window), `ChatRoomAccessPolicy` (role-based room access)

#### Application Layer
- **Repository Interfaces:** `IAttendanceRepository`, `IContentItemRepository`, `IChatRoomRepository`, `IChatMessageRepository`
- **Service Interfaces:** `IAttendanceService`, `IContentService`, `IChatService`
- **DTOs:** `AttendanceDto`, `SessionAttendanceDto`, `SubjectAttendanceDto`, `ContentItemDto`, `ChatRoomDto`, `ChatMessageDto`, `UploadContentUrlRequest`, `OverrideAttendanceRequest`

#### Infrastructure Layer
- **Repositories:** `AttendanceRepository`, `ContentItemRepository`, `ChatRoomRepository`, `ChatMessageRepository`
- **Services:** `AttendanceService`, `ContentService`, `ChatService`
- **Background Job:** `AbsentMarkingBackgroundJob` (runs every 5 minutes, marks absent after session end + 15min grace)
- **EF Core:** Updated `Context.cs` with 3 new DbSets, 3 new table configurations, updated Attendance configuration
- **Seeder:** `SeedChatRoomsAsync` added to `DatabaseSeeder` (1 room per grade + 1 teacher room)

#### API Layer
- **Controllers:** `StudentAttendanceController`, `TeacherAttendanceController`, `ParentAttendanceController`, `TeacherContentController`, `StudentContentController`, `ContentDownloadController`, `ChatController`
- **SignalR Hub:** `ChatHub` at `/hubs/chat` (JoinRoom, LeaveRoom, SendMessage with ReceiveMessage/UserJoined/UserLeft client events)
- **Program.cs:** Added SignalR with Redis backplane, AbsentMarkingBackgroundJob, chat room seeding, hub endpoint mapping
- **JWT Config:** Added OnMessageReceived for SignalR query string token forwarding
- **NuGet:** Added `Microsoft.AspNetCore.SignalR.StackExchangeRedis`

#### API Endpoints Implemented
| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/student/sessions/{sessionId}/join` | Record student attendance |
| GET | `/api/student/attendance` | Student attendance summary |
| GET | `/api/teacher/sessions/{sessionId}/attendance` | Session attendance roster |
| PUT | `/api/teacher/attendance/{attendanceId}/override` | Override attendance |
| GET | `/api/parent/children/{studentId}/attendance` | Parent view child attendance |
| POST | `/api/teacher/content/url` | Upload URL content |
| POST | `/api/teacher/content/file` | Upload file content |
| GET | `/api/teacher/content/{taId}` | Get teacher content |
| DELETE | `/api/teacher/content/{id}` | Delete content |
| GET | `/api/student/content/{subjectId}` | Browse subject content |
| GET | `/api/content/{id}/download-url` | Get signed download URL |
| GET | `/api/chat/rooms` | Get accessible rooms |
| GET | `/api/chat/rooms/{roomId}/messages` | Get room messages |
| DELETE | `/api/chat/messages/{messageId}` | Delete message |
| WS | `/hubs/chat` | SignalR ChatHub |

#### Files Modified/Created
**Domain:**
- `Masarak.Domain/Entities/Attendance.cs` (modified)
- `Masarak.Domain/Entities/ContentItem.cs` (new)
- `Masarak.Domain/Entities/ChatRoom.cs` (new)
- `Masarak.Domain/Entities/ChatMessage.cs` (new)
- `Masarak.Domain/Entities/Student.cs` (modified - removed stale Attendances nav)
- `Masarak.Domain/Enums/AttendanceStatus.cs` (new)
- `Masarak.Domain/Enums/ContentType.cs` (new)
- `Masarak.Domain/Enums/ContentSourceType.cs` (new)
- `Masarak.Domain/Enums/ChatRoomType.cs` (new)
- `Masarak.Domain/Services/AttendanceWindowChecker.cs` (new)
- `Masarak.Domain/Services/ChatRoomAccessPolicy.cs` (new)

**Application:**
- `Masarak.Application/Interfaces/IAttendanceRepository.cs` (new)
- `Masarak.Application/Interfaces/IContentItemRepository.cs` (new)
- `Masarak.Application/Interfaces/IChatRoomRepository.cs` (new)
- `Masarak.Application/Interfaces/IChatMessageRepository.cs` (new)
- `Masarak.Application/Interfaces/IAttendanceService.cs` (new)
- `Masarak.Application/Interfaces/IContentService.cs` (new)
- `Masarak.Application/Interfaces/IChatService.cs` (new)
- `Masarak.Application/DTOs/Phase4DTOs.cs` (new)

**Infrastructure:**
- `Masarak.Infrastructure/Persistence/Context.cs` (modified)
- `Masarak.Infrastructure/Persistence/Repositories/AttendanceRepository.cs` (new)
- `Masarak.Infrastructure/Persistence/Repositories/ContentItemRepository.cs` (new)
- `Masarak.Infrastructure/Persistence/Repositories/ChatRoomRepository.cs` (new)
- `Masarak.Infrastructure/Persistence/Repositories/ChatMessageRepository.cs` (new)
- `Masarak.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs` (modified)
- `Masarak.Infrastructure/Services/AttendanceService.cs` (new)
- `Masarak.Infrastructure/Services/ContentService.cs` (new)
- `Masarak.Infrastructure/Services/ChatService.cs` (new)
- `Masarak.Infrastructure/Services/AbsentMarkingBackgroundJob.cs` (new)

**API:**
- `Masarak.API/Program.cs` (modified)
- `Masarak.API/Masarak.API.csproj` (modified)
- `Masarak.API/Extensions/ServiceCollectionExtensions.cs` (modified)
- `Masarak.API/Hubs/ChatHub.cs` (new)
- `Masarak.API/Controllers/AttendanceController.cs` (new)
- `Masarak.API/Controllers/ContentController.cs` (new)
- `Masarak.API/Controllers/ChatController.cs` (new)

#### Validation
- ✅ Build succeeded with 0 errors
- ✅ All Phase 4 entities, services, and controllers implemented
- ✅ All API endpoints from Phase 4 spec implemented
- ✅ SignalR hub configured with Redis backplane support
- ✅ Background job for absent marking implemented
- ✅ Chat room seeder integrated into startup pipeline

#### Business Impact
- Students can now join live sessions with automatic attendance tracking
- Teachers can view attendance rosters and override records
- Parents can monitor their children's attendance per subject
- Teachers can upload video and document content linked to subjects
- Students can browse and access content materials for their enrolled classes
- Community chat enables real-time communication per grade and among teachers

#### Remaining / Next Steps
- Phase 6: Notifications, Admin CRUD & Angular Shell
- ✅ EF Core migration generated and applied for the Phase 4 schema changes
- ✅ EF Core migration generated and applied for the Phase 5 schema changes

### ✅ Phase 5: AI Recommendations, Smart Reports & Analytics (Completed 2026-06-27)

#### Domain Layer
- **Entities:** Refactored `AiRecommendation` (new PK, Type enum, Payload JSON, ProviderUsed, token tracking, TTL/ExpiresAt, IsActive); Created `PerformanceAlert`, `AiPromptTemplate`, `AnalyticsDashboardSnapshot`
- **Enums:** `RecommendationType`, `AlertType`, `SnapshotScope`, `AiProvider`
- **Value Objects:** `WeaknessAnalysisResult`, `WeakTopic`, `ContentRecommendationResult`, `ParentReportData`, `SubjectSummary`
- **Events:** `AlertCreatedEvent`, `ParentReportReadyEvent`

#### Application Layer
- **Repository Interfaces:** `IAiRecommendationRepository`, `IPerformanceAlertRepository`, `IAiPromptTemplateRepository`, `IAnalyticsSnapshotRepository`
- **Service Interfaces:** `IAiProvider`, `IAiProviderFactory`, `IAiAnalyticsService`
- **DTOs:** `WeaknessAnalysisDto`, `WeakTopicDto`, `ContentRecommendationDto`, `ParentReportDto`, `SubjectSummaryDto`, `TeachingSuggestionDto`, `LearningInsightsDashboardDto`, `PerformanceTrendDto`, `ScorePointDto`, `ClassAnalyticsDashboardDto`, `ScoreDistributionBucketDto`, `AnalyticsStudentScoreDto`, `PlatformAnalyticsDto`, `GradeEnrollmentDto`, `GradeHeatmapDto`, `ClassHeatmapRowDto`, `SubjectScoreCellDto`, `PerformanceAlertDto`, `AiPromptTemplateDto`, `UpdatePromptTemplateRequest`, `AiPromptRequest`, `AiCompletionResult`, `GenerateParentReportRequest`

#### Infrastructure Layer
- **Repositories:** `AiRecommendationRepository`, `PerformanceAlertRepository`, `AiPromptTemplateRepository`, `AnalyticsSnapshotRepository`
- **AI Providers:** `OpenAiProvider` (GPT-4o), `ClaudeProvider` (Claude Sonnet 4), `GeminiProvider` (Gemini 1.5 Flash)
- **AI Factory:** `AiProviderFactory` with configurable primary + fallback chain (OpenAI → Claude → Gemini)
- **AI Service:** `AiAnalyticsService` — orchestrates all dashboards, AI generation, caching, and rule-based alerts
- **MassTransit Consumer:** `PerformanceRecalculatedAiConsumer` — invalidates cache, triggers weakness analysis + alert evaluation
- **Seeder:** `SeedAiPromptTemplatesAsync` — seeds 3 prompt templates (weakness_analysis, parent_report, teaching_suggestion)
- **EF Core:** Updated `Context.cs` with 3 new DbSets, 4 new/updated table configurations, composite indexes

#### API Layer
- **Controllers:** `StudentInsightsController`, `ParentReportsController`, `TeacherAnalyticsController`, `AdminAnalyticsController`
- **Program.cs:** Added Phase 5 MassTransit consumer, prompt template seeding
- **DI:** Registered all Phase 5 repositories, AI providers (HttpClient-based), factory, and analytics service

#### API Endpoints Implemented
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/student/insights` | Student learning insights dashboard |
| GET | `/api/student/recommendations/{subjectId}` | AI-recommended content per subject |
| GET | `/api/parent/reports/{studentId}/{month}` | Get cached parent report |
| POST | `/api/parent/reports/{studentId}/generate` | Generate fresh parent report (24h cache) |
| GET | `/api/parent/children/{studentId}/alerts` | Get student performance alerts |
| GET | `/api/teacher/analytics/{classId}/{subjectId}` | Class analytics dashboard |
| POST | `/api/teacher/students/{studentId}/suggestions/{subjectId}` | AI teaching suggestion |
| GET | `/api/admin/analytics/platform` | Platform-wide KPI analytics |
| GET | `/api/admin/analytics/grades/{gradeId}/heatmap` | Grade performance heatmap |
| GET | `/api/admin/ai/prompt-templates` | List AI prompt templates |
| PUT | `/api/admin/ai/prompt-templates/{key}` | Update prompt template |

#### Files Modified/Created
**Domain:**
- `Masarak.Domain/Entities/AiRecommendation.cs` (refactored)
- `Masarak.Domain/Entities/PerformanceAlert.cs` (new)
- `Masarak.Domain/Entities/AiPromptTemplate.cs` (new)
- `Masarak.Domain/Entities/AnalyticsDashboardSnapshot.cs` (new)
- `Masarak.Domain/Entities/Student.cs` (modified — removed stale AiRecommendations nav)
- `Masarak.Domain/Enums/Phase5Enums.cs` (new)
- `Masarak.Domain/ValueObjects/Phase5ValueObjects.cs` (new)
- `Masarak.Domain/Events/DomainEvents.cs` (modified — added Phase 5 events)

**Application:**
- `Masarak.Application/Interfaces/IAiProvider.cs` (new)
- `Masarak.Application/Interfaces/IPhase5Repositories.cs` (new)
- `Masarak.Application/Interfaces/IAiAnalyticsService.cs` (new)
- `Masarak.Application/DTOs/Phase5DTOs.cs` (new)

**Infrastructure:**
- `Masarak.Infrastructure/Persistence/Context.cs` (modified — 3 new DbSets, 4 entity configs)
- `Masarak.Infrastructure/Persistence/Repositories/Phase5Repositories.cs` (new)
- `Masarak.Infrastructure/Persistence/Seeders/DatabaseSeeder.cs` (modified — added SeedAiPromptTemplatesAsync)
- `Masarak.Infrastructure/Services/AI/AiProviders.cs` (new)
- `Masarak.Infrastructure/Services/AI/AiAnalyticsService.cs` (new)
- `Masarak.Infrastructure/Messaging/Phase5Consumers.cs` (new)

**API:**
- `Masarak.API/Program.cs` (modified — Phase 5 consumer + seeder)
- `Masarak.API/Extensions/ServiceCollectionExtensions.cs` (modified — Phase 5 DI)
- `Masarak.API/Controllers/StudentInsightsController.cs` (new)
- `Masarak.API/Controllers/ParentReportsController.cs` (new)
- `Masarak.API/Controllers/TeacherAnalyticsController.cs` (new)
- `Masarak.API/Controllers/AdminAnalyticsController.cs` (new)

#### Validation
- ✅ Build succeeded with 0 errors
- ✅ All Phase 5 entities, services, and controllers implemented
- ✅ All 11 API endpoints from Phase 5 spec implemented
- ✅ AI provider abstraction with configurable fallback chain
- ✅ Rule-based alerts at 75% attendance and 50% exam score thresholds
- ✅ Redis caching for AI results (24h parent reports, weakness analysis invalidation)
- ✅ Prompt template seeder with 3 default templates
- ✅ MassTransit consumer bridges performance events to AI pipeline

#### Business Impact
- Students can view AI-powered learning insights with weakness analyses and content recommendations
- Parents can generate and view monthly smart reports with AI narratives for linked children
- Parents receive automatic alerts when children's attendance or exam scores drop below thresholds
- Teachers can view class analytics dashboards with score distributions and top/bottom students
- Teachers can request AI-generated teaching suggestions for individual students
- Admin has platform-wide analytics (students, teachers, revenue, session completion rate)
- Admin can view grade-level performance heatmaps with color-coded subject scores
- Admin can edit AI prompt templates without redeployment

### ✅ Phase 6: Notifications & Admin API (Completed)

#### Domain Layer
- **Entities:** Refactored `Notification` (added Factory method, NotificationType enum, Channel enum, ActionUrl).
- **Enums:** `NotificationType`, `NotificationChannel`.
- **Events:** Added `SubscriptionExpiringEvent`, `PaymentFailedEvent`, `SessionScheduledEvent`, `StudentEnrolledEvent`.

#### Application Layer
- **Repository Interfaces:** `INotificationRepository`, `IAdminUserRepository`.
- **Service Interfaces:** `INotificationPushService`, `INotificationService`.
- **DTOs:** `NotificationDto`, `AdminUserDto`, `AdminUserDetailDto`, `SystemHealthDto`.

#### Infrastructure Layer
- **Repositories:** `NotificationRepository`, `AdminUserRepository`.
- **Services:** `NotificationService` (orchestrates creation and SignalR push).
- **Messaging (MassTransit):** 8 event consumers implemented to cover notifications across all phases (SubscriptionActivated, SubscriptionExpiring, ExamFullyGraded, AssignmentGraded, SessionScheduled, AlertCreated, ParentReportReady, PaymentFailed).
- **EF Core:** Added optimized composite indexes to `Context.cs` for notifications. Migration generated.

#### API Layer
- **SignalR Hub:** `NotificationHub` at `/hubs/notifications`.
- **Controllers:** `NotificationsController`, `AdminUsersController`, `AdminContentModerationController`, `AdminSystemController`.
- **Services:** `SignalRNotificationPushService` (moved to API layer to prevent circular dependency).
- **DI/Middleware:** All services registered in `ServiceCollectionExtensions.cs`.

#### Angular Platform Shell
- *Pending frontend initialization.*

#### Validation
- ✅ Build succeeded with 0 errors.
- ✅ All Phase 6 backend entities, services, consumers, and controllers implemented.
- ✅ Migration generated (pending DB update due to SQL Server connection).
