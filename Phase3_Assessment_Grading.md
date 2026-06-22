# Phase 3 — Assessment & Grading: Assignments, Exams, Submissions & Auto/Manual Grading

**Developer:** Dev 3  
**Complexity:** Very High  
**Dependencies:** Phase 1 (Auth, Subscription), Phase 2 (TeachingAssignment, StudentClass, Session entities must exist as FK targets)

---

## Objective

Deliver the complete assessment vertical slice. Teachers create assignments and exams with typed questions (MCQ, True/False, Short Answer, Essay, Fill-in-the-Blank, File Upload). Students submit answers. The system auto-grades objective question types (MCQ, T/F, FIB) immediately on submission and queues subjective types (Short Answer, Essay, File Upload) for teacher review. Teachers review and grade pending submissions. Final scores are computed and stored as StudentPerformance records. Every operation enforces that the acting student/teacher is enrolled or assigned in the relevant class.

---

## 1. Functional Requirements

- Teacher creates an **Assignment** linked to a TeachingAssignment (class+subject), with a due date and optional instructions
- Teacher creates an **Exam** linked to a TeachingAssignment, with a time window (open/close datetime) and total marks
- Teacher adds questions to an exam: MCQ, True/False, Short Answer, Essay, Fill-in-the-Blank, File Upload
- Each question has a marks value; exam total marks = sum of all question marks
- Teacher publishes a Question Bank: reusable questions tagged by subject and difficulty level
- Student views available assignments/exams for their enrolled class
- Student submits an assignment (text or file upload, or both)
- Student starts an exam attempt within the time window; once started, a countdown timer enforces the exam duration
- Student answers questions and submits; partial saves allowed during the exam window
- On submission: MCQ, T/F, FIB are auto-graded immediately; Short Answer, Essay, File Upload are marked `PendingReview`
- Teacher reviews pending submissions question by question, enters a score and optional feedback per question
- On full grading completion, `StudentPerformance` record is updated with the final score
- One exam attempt per student per exam (no retakes unless teacher explicitly resets)
- File upload answers stored in Azure Blob Storage; a signed URL is issued for teacher download

---

## 2. Domain Layer (`Masarak.Domain`)

### Entities

```csharp
public class Assignment
{
    public int AssignmentId { get; private set; }
    public int TeachingAssignmentId { get; private set; }
    public string Title { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime DueDate { get; private set; }
    public int MaxMarks { get; private set; }
    public AssignmentStatus Status { get; private set; } // Draft, Published, Closed
    public DateTime CreatedAt { get; private set; }
    public TeachingAssignment TeachingAssignment { get; private set; }
    public ICollection<Submission> Submissions { get; private set; }

    public static Assignment Create(int teachingAssignmentId, string title, string? instructions,
        DateTime dueDate, int maxMarks) { ... }
    public void Publish() { Status = AssignmentStatus.Published; }
    public void Close() { Status = AssignmentStatus.Closed; }
}

public class Submission
{
    public int SubmissionId { get; private set; }
    public int AssignmentId { get; private set; }
    public int StudentUserId { get; private set; }
    public string? TextContent { get; private set; }
    public string? FileUrl { get; private set; }         // Azure Blob URL
    public string? FileBlobName { get; private set; }    // internal blob name for signed URL generation
    public SubmissionStatus Status { get; private set; } // Submitted, Graded
    public int? MarksAwarded { get; private set; }
    public string? TeacherFeedback { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? GradedAt { get; private set; }
    public Assignment Assignment { get; private set; }

    public static Submission Create(int assignmentId, int studentUserId,
        string? textContent, string? fileUrl, string? fileBlobName) { ... }
    public void Grade(int marks, string? feedback) { ... }
}

public class Exam
{
    public int ExamId { get; private set; }
    public int TeachingAssignmentId { get; private set; }
    public string Title { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime OpenAt { get; private set; }
    public DateTime CloseAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public int TotalMarks { get; private set; }          // computed from questions sum
    public ExamStatus Status { get; private set; }       // Draft, Published, Closed
    public DateTime CreatedAt { get; private set; }
    public TeachingAssignment TeachingAssignment { get; private set; }
    public ICollection<Question> Questions { get; private set; }
    public ICollection<StudentExam> StudentExams { get; private set; }

    public static Exam Create(int teachingAssignmentId, string title, string? instructions,
        DateTime openAt, DateTime closeAt, int durationMinutes) { ... }
    public void Publish() { /* validate at least 1 question exists */ }
    public void RecalculateTotalMarks() { TotalMarks = Questions.Sum(q => q.Marks); }
}

public class Question
{
    public int QuestionId { get; private set; }
    public int? ExamId { get; private set; }             // null if question bank only
    public int? QuestionBankId { get; private set; }     // source bank entry
    public int SubjectId { get; private set; }
    public QuestionType Type { get; private set; }       // enum (see below)
    public string Text { get; private set; }
    public string? ImageUrl { get; private set; }
    public int Marks { get; private set; }
    public DifficultyLevel Difficulty { get; private set; } // Easy, Medium, Hard
    public int Order { get; private set; }
    // MCQ/FIB options stored as owned collection
    public ICollection<QuestionOption> Options { get; private set; }
    public string? CorrectAnswer { get; private set; }   // MCQ: option id; T/F: "True"/"False"; FIB: exact string
    public bool IsAutoGraded => Type is QuestionType.MCQ or QuestionType.TrueFalse or QuestionType.FillInBlank;
    public Exam? Exam { get; private set; }
    public ICollection<StudentAnswer> StudentAnswers { get; private set; }
}

public class QuestionOption
{
    public int QuestionOptionId { get; private set; }
    public int QuestionId { get; private set; }
    public string Text { get; private set; }
    public char Label { get; private set; }              // 'A', 'B', 'C', 'D'
    public Question Question { get; private set; }
}

public class StudentExam
{
    public int StudentExamId { get; private set; }
    public int ExamId { get; private set; }
    public int StudentUserId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }      // StartedAt + DurationMinutes
    public StudentExamStatus Status { get; private set; } // InProgress, Submitted, AutoExpired, Graded
    public int? TotalAutoScore { get; private set; }
    public int? TotalManualScore { get; private set; }
    public int? FinalScore { get; private set; }         // auto + manual
    public bool HasPendingManualGrading { get; private set; }
    public Exam Exam { get; private set; }
    public ICollection<StudentAnswer> Answers { get; private set; }

    public static StudentExam Begin(int examId, int studentUserId, int durationMinutes) { ... }
    public void Submit(IEnumerable<StudentAnswer> answers) { ... }
    public void ApplyAutoGrading(int autoScore) { ... }
    public void FinalizeMixedGrading(int manualScore) { FinalScore = TotalAutoScore + manualScore; HasPendingManualGrading = false; Status = StudentExamStatus.Graded; }
}

public class StudentAnswer
{
    public int StudentAnswerId { get; private set; }
    public int StudentExamId { get; private set; }
    public int QuestionId { get; private set; }
    public string? AnswerText { get; private set; }      // for text-based types
    public string? SelectedOptionId { get; private set; }// for MCQ
    public string? FileBlobName { get; private set; }    // for file upload
    public string? FileUrl { get; private set; }
    public AnswerGradingStatus GradingStatus { get; private set; } // AutoGraded, PendingReview, ManuallyGraded
    public int? MarksAwarded { get; private set; }
    public string? TeacherFeedback { get; private set; }
    public StudentExam StudentExam { get; private set; }
    public Question Question { get; private set; }

    public void AutoGrade(string correctAnswer) { ... } // sets MarksAwarded and GradingStatus
    public void ManualGrade(int marks, string? feedback) { ... }
}

public class StudentPerformance
{
    public int StudentPerformanceId { get; private set; }
    public int StudentUserId { get; private set; }
    public int SubjectId { get; private set; }
    public int ClassId { get; private set; }
    public int AcademicYear { get; private set; }
    public decimal AverageExamScore { get; private set; }   // 0-100 percentage
    public decimal AverageAssignmentScore { get; private set; }
    public int TotalExamsTaken { get; private set; }
    public int TotalAssignmentsSubmitted { get; private set; }
    public int TotalAssignmentsPending { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    // Recalculated whenever a grading event fires
    public void Recalculate(IEnumerable<StudentExam> exams, IEnumerable<Submission> submissions) { ... }
}
```

### Domain Services

```csharp
public class AutoGradingService
{
    // Evaluates a StudentAnswer against its Question's CorrectAnswer
    // MCQ: string equality on selected option label
    // TrueFalse: "True" / "False" equality (case-insensitive)
    // FillInBlank: normalized string equality (trim, lowercase, ignore punctuation)
    // Returns marks awarded (full marks or 0 — no partial for auto-graded)
    public int Grade(Question question, StudentAnswer answer) { ... }
}

public class ExamTimerEnforcer
{
    // Called on any save-answer or submit request
    // Returns true if StudentExam.ExpiresAt > DateTime.UtcNow
    // If expired, auto-submits whatever answers exist
    public bool IsWithinTimeWindow(StudentExam attempt) => DateTime.UtcNow < attempt.ExpiresAt;
}
```

### Enums

```csharp
public enum QuestionType { MCQ, TrueFalse, ShortAnswer, Essay, FillInBlank, FileUpload }
public enum DifficultyLevel { Easy, Medium, Hard }
public enum AssignmentStatus { Draft, Published, Closed }
public enum ExamStatus { Draft, Published, Closed }
public enum StudentExamStatus { InProgress, Submitted, AutoExpired, Graded }
public enum SubmissionStatus { Submitted, Graded }
public enum AnswerGradingStatus { AutoGraded, PendingReview, ManuallyGraded }
```

### Domain Events

```csharp
public record ExamSubmittedEvent(int StudentExamId, int StudentUserId, int ExamId);
public record ExamFullyGradedEvent(int StudentExamId, int StudentUserId, int SubjectId, int ClassId);
public record AssignmentGradedEvent(int SubmissionId, int StudentUserId, int SubjectId, int ClassId);
public record PerformanceRecalculatedEvent(int StudentUserId, int SubjectId, int ClassId);
```

---

## 3. Application Layer (`Masarak.Application`)

### Interfaces

```csharp
public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<Assignment>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct);
    Task<IEnumerable<Assignment>> GetPublishedForClassAsync(int classId, int subjectId, CancellationToken ct);
    Task AddAsync(Assignment assignment, CancellationToken ct);
    Task UpdateAsync(Assignment assignment, CancellationToken ct);
}

public interface IExamRepository
{
    Task<Exam?> GetByIdWithQuestionsAsync(int examId, CancellationToken ct);
    Task<IEnumerable<Exam>> GetByTeachingAssignmentIdAsync(int taId, CancellationToken ct);
    Task<IEnumerable<Exam>> GetOpenExamsForClassAsync(int classId, int subjectId, DateTime now, CancellationToken ct);
    Task AddAsync(Exam exam, CancellationToken ct);
    Task UpdateAsync(Exam exam, CancellationToken ct);
}

public interface IStudentExamRepository
{
    Task<StudentExam?> GetByStudentAndExamAsync(int studentUserId, int examId, CancellationToken ct);
    Task<StudentExam?> GetByIdWithAnswersAsync(int studentExamId, CancellationToken ct);
    Task<IEnumerable<StudentExam>> GetPendingManualGradingForTeacherAsync(int teacherUserId, CancellationToken ct);
    Task AddAsync(StudentExam attempt, CancellationToken ct);
    Task UpdateAsync(StudentExam attempt, CancellationToken ct);
}

public interface ISubmissionRepository
{
    Task<Submission?> GetByStudentAndAssignmentAsync(int studentUserId, int assignmentId, CancellationToken ct);
    Task<IEnumerable<Submission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken ct);
    Task AddAsync(Submission submission, CancellationToken ct);
    Task UpdateAsync(Submission submission, CancellationToken ct);
}

public interface IStudentPerformanceRepository
{
    Task<StudentPerformance?> GetByStudentSubjectClassAsync(int studentUserId, int subjectId, int classId, int year, CancellationToken ct);
    Task UpsertAsync(StudentPerformance performance, CancellationToken ct);
}

public interface IFileStorageService
{
    Task<(string BlobName, string PublicUrl)> UploadAsync(Stream content, string fileName, string containerName, CancellationToken ct);
    Task<string> GenerateSignedDownloadUrlAsync(string blobName, string containerName, TimeSpan expiry, CancellationToken ct);
    Task DeleteAsync(string blobName, string containerName, CancellationToken ct);
}
```

### Commands

```csharp
// Assignment lifecycle (Teacher)
public record CreateAssignmentCommand(int TeacherUserId, int TeachingAssignmentId, string Title,
    string? Instructions, DateTime DueDate, int MaxMarks) : IRequest<AssignmentDto>;
public record PublishAssignmentCommand(int TeacherUserId, int AssignmentId) : IRequest<Unit>;
public record CloseAssignmentCommand(int TeacherUserId, int AssignmentId) : IRequest<Unit>;

// Assignment submission (Student)
public record SubmitAssignmentCommand(int StudentUserId, int AssignmentId,
    string? TextContent, Stream? FileStream, string? FileName) : IRequest<SubmissionDto>;
public record GradeSubmissionCommand(int TeacherUserId, int SubmissionId,
    int MarksAwarded, string? Feedback) : IRequest<SubmissionDto>;

// Exam lifecycle (Teacher)
public record CreateExamCommand(int TeacherUserId, int TeachingAssignmentId, string Title,
    string? Instructions, DateTime OpenAt, DateTime CloseAt, int DurationMinutes) : IRequest<ExamDto>;
public record AddQuestionToExamCommand(int TeacherUserId, int ExamId, QuestionType Type,
    string Text, int Marks, DifficultyLevel Difficulty, int Order,
    IEnumerable<QuestionOptionDto>? Options, string? CorrectAnswer, string? ImageUrl) : IRequest<QuestionDto>;
public record UpdateQuestionCommand(int TeacherUserId, int QuestionId, string Text,
    int Marks, IEnumerable<QuestionOptionDto>? Options, string? CorrectAnswer) : IRequest<QuestionDto>;
public record RemoveQuestionCommand(int TeacherUserId, int QuestionId) : IRequest<Unit>;
public record PublishExamCommand(int TeacherUserId, int ExamId) : IRequest<Unit>;

// Exam taking (Student)
public record StartExamAttemptCommand(int StudentUserId, int ExamId) : IRequest<ExamAttemptDto>;
public record SaveAnswerCommand(int StudentUserId, int StudentExamId, int QuestionId,
    string? AnswerText, string? SelectedOptionId, Stream? FileStream, string? FileName) : IRequest<Unit>;
public record SubmitExamCommand(int StudentUserId, int StudentExamId) : IRequest<ExamResultDto>;

// Manual grading (Teacher)
public record GradeStudentAnswerCommand(int TeacherUserId, int StudentAnswerId,
    int MarksAwarded, string? Feedback) : IRequest<Unit>;
```

### Queries

```csharp
// Teacher
public record GetMyAssignmentsQuery(int TeacherUserId, int TeachingAssignmentId) : IRequest<IEnumerable<AssignmentDto>>;
public record GetSubmissionsForAssignmentQuery(int TeacherUserId, int AssignmentId) : IRequest<IEnumerable<SubmissionDetailDto>>;
public record GetPendingGradingQuery(int TeacherUserId) : IRequest<PendingGradingDashboardDto>;
public record GetStudentAnswersForReviewQuery(int TeacherUserId, int StudentExamId) : IRequest<ExamGradingReviewDto>;
public record GetMyExamsQuery(int TeacherUserId, int TeachingAssignmentId) : IRequest<IEnumerable<ExamDto>>;

// Student
public record GetMyAssignmentsForSubjectQuery(int StudentUserId, int SubjectId) : IRequest<IEnumerable<AssignmentDto>>;
public record GetOpenExamsForSubjectQuery(int StudentUserId, int SubjectId) : IRequest<IEnumerable<ExamDto>>;
public record GetMyExamAttemptQuery(int StudentUserId, int ExamId) : IRequest<ExamAttemptDto?>;
public record GetMyExamResultQuery(int StudentUserId, int StudentExamId) : IRequest<ExamResultDto>;
public record GetMyPerformanceQuery(int StudentUserId, int ClassId, int AcademicYear) : IRequest<IEnumerable<SubjectPerformanceDto>>;

// Admin
public record GetClassPerformanceReportQuery(int ClassId, int SubjectId, int AcademicYear) : IRequest<ClassPerformanceReportDto>;
```

### DTOs

```csharp
public record AssignmentDto(int AssignmentId, string Title, DateTime DueDate, int MaxMarks, AssignmentStatus Status, string SubjectName, string ClassName, int SubmissionCount);
public record SubmissionDetailDto(int SubmissionId, string StudentName, SubmissionStatus Status, int? MarksAwarded, DateTime SubmittedAt, bool HasFile);
public record ExamDto(int ExamId, string Title, DateTime OpenAt, DateTime CloseAt, int DurationMinutes, int TotalMarks, ExamStatus Status, int QuestionCount);
public record QuestionDto(int QuestionId, QuestionType Type, string Text, int Marks, DifficultyLevel Difficulty, int Order, IEnumerable<QuestionOptionDto>? Options);
public record QuestionOptionDto(string Label, string Text);
public record ExamAttemptDto(int StudentExamId, int ExamId, string ExamTitle, DateTime ExpiresAt, int SecondsRemaining, IEnumerable<QuestionDto> Questions, IEnumerable<SavedAnswerDto> SavedAnswers);
public record SavedAnswerDto(int QuestionId, string? AnswerText, string? SelectedOptionId);
public record ExamResultDto(int StudentExamId, int FinalScore, int TotalMarks, decimal Percentage, bool HasPendingManualGrading, IEnumerable<AnswerResultDto> Answers);
public record AnswerResultDto(int QuestionId, string QuestionText, string? YourAnswer, string? CorrectAnswer, int MarksAwarded, int MaxMarks, AnswerGradingStatus GradingStatus);
public record SubjectPerformanceDto(int SubjectId, string SubjectName, decimal AverageExamScore, decimal AverageAssignmentScore, int TotalExamsTaken, int TotalAssignmentsSubmitted);
public record PendingGradingDashboardDto(int TotalPendingExamAnswers, int TotalPendingSubmissions, IEnumerable<PendingExamSummaryDto> Exams, IEnumerable<PendingAssignmentSummaryDto> Assignments);
public record ClassPerformanceReportDto(int ClassId, string ClassName, string SubjectName, decimal ClassAverageScore, IEnumerable<StudentScoreDto> StudentScores);
```

### Critical Handler Logic Notes

**`StartExamAttemptHandler`**
1. Verify student is enrolled in the class for this exam's teaching assignment
2. Verify exam `Status == Published` and `DateTime.UtcNow` is between `OpenAt` and `CloseAt`
3. Check no existing `StudentExam` for this student/exam — if exists and `InProgress`, return existing attempt with remaining time
4. If exists and `Submitted/Graded`, return `409` "Exam already submitted"
5. Create `StudentExam.Begin(examId, studentUserId, durationMinutes)`
6. Return `ExamAttemptDto` with questions (shuffle order via `Question.Order` randomized per attempt)

**`SubmitExamHandler`**
1. Load `StudentExam` with all answers, verify it belongs to calling student
2. Verify `ExamTimerEnforcer.IsWithinTimeWindow` — if expired, auto-submit with whatever was saved
3. Auto-grade all `IsAutoGraded` questions using `AutoGradingService`
4. Sum auto scores, set `TotalAutoScore`
5. If all questions are auto-graded: set `FinalScore`, `Status = Graded`, `HasPendingManualGrading = false`
6. If any subjective questions exist: `HasPendingManualGrading = true`, `Status = Submitted`
7. Publish `ExamSubmittedEvent` → RabbitMQ for async performance recalculation

**`GradeStudentAnswerHandler`**
1. Verify teacher owns the exam via TeachingAssignment
2. Load `StudentAnswer`, verify `GradingStatus == PendingReview`
3. Validate marks ≤ question's max marks
4. Call `answer.ManualGrade(marks, feedback)`
5. Check if all answers in the `StudentExam` are now graded
6. If yes: call `studentExam.FinalizeMixedGrading(totalManualScore)`, publish `ExamFullyGradedEvent`

**Performance Recalculation (RabbitMQ consumer)**
- Consumes `ExamFullyGradedEvent` and `AssignmentGradedEvent`
- Loads all `StudentExam` (graded) and `Submission` (graded) for student+subject+class
- Calls `StudentPerformance.Recalculate(...)`
- Upserts via `IStudentPerformanceRepository`
- Publishes `PerformanceRecalculatedEvent` → consumed by AI/Analytics phase

---

## 4. Infrastructure Layer

### EF Core Configurations

```csharp
// Table names: assignments, submissions, exams, questions, question_options,
//              student_exams, student_answers, student_performances

// ExamConfiguration
builder.HasIndex(e => new { e.TeachingAssignmentId, e.OpenAt });

// QuestionConfiguration
builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
builder.Property(q => q.Difficulty).HasConversion<string>().HasMaxLength(10);
builder.HasQueryFilter(q => q.ExamId != null); // default filter excludes question bank entries

// StudentExamConfiguration
builder.HasIndex(se => new { se.StudentUserId, se.ExamId }).IsUnique();
builder.Property(se => se.Status).HasConversion<string>().HasMaxLength(20);

// StudentAnswerConfiguration
builder.Property(sa => sa.GradingStatus).HasConversion<string>().HasMaxLength(20);

// StudentPerformanceConfiguration
builder.HasIndex(sp => new { sp.StudentUserId, sp.SubjectId, sp.ClassId, sp.AcademicYear }).IsUnique();
builder.Property(sp => sp.AverageExamScore).HasPrecision(5, 2);
builder.Property(sp => sp.AverageAssignmentScore).HasPrecision(5, 2);
```

### Azure Blob Storage Service

```csharp
// NuGet: Azure.Storage.Blobs
public class AzureBlobStorageService : IFileStorageService
{
    // Constructor injects IConfiguration for Azure:BlobStorage:ConnectionString
    // Containers: "submissions" (private), "question-images" (public read)
    // UploadAsync: uploads to named container, returns (blobName, url)
    // GenerateSignedDownloadUrlAsync: generates SAS URL valid for specified duration
    // DeleteAsync: deletes blob from container
}
```

### RabbitMQ Integration (Async Performance Recalculation)

```csharp
// NuGet: MassTransit.RabbitMQ
// Queue: masarak.performance.recalculation
// Publisher: IPublishEndpoint.Publish(new ExamFullyGradedEvent(...))
// Consumer: PerformanceRecalculationConsumer : IConsumer<ExamFullyGradedEvent>
//           PerformanceRecalculationConsumer : IConsumer<AssignmentGradedEvent>
// Retry policy: 3 retries with exponential backoff; dead-letter queue on final failure
```

### Auto-Expire Background Job

```csharp
// Runs every 5 minutes
// Queries StudentExam where Status = InProgress AND ExpiresAt < UtcNow
// Auto-submits each: grades auto-gradeable questions, marks subjective as PendingReview
// Publishes ExamSubmittedEvent for each
public class ExamAutoExpireJob : BackgroundService { ... }
```

---

## 5. API Endpoints

```
// Teacher — Assignments
POST   /api/teacher/assignments                        → CreateAssignmentCommand
PUT    /api/teacher/assignments/{id}/publish           → PublishAssignmentCommand
PUT    /api/teacher/assignments/{id}/close             → CloseAssignmentCommand
GET    /api/teacher/assignments/{taId}                 → GetMyAssignmentsQuery
GET    /api/teacher/assignments/{id}/submissions       → GetSubmissionsForAssignmentQuery
POST   /api/teacher/submissions/{id}/grade             → GradeSubmissionCommand
GET    /api/teacher/submissions/{id}/download          → GenerateSignedDownloadUrl (returns signed Azure URL)

// Teacher — Exams
POST   /api/teacher/exams                              → CreateExamCommand
POST   /api/teacher/exams/{id}/questions               → AddQuestionToExamCommand
PUT    /api/teacher/exams/{id}/questions/{qid}         → UpdateQuestionCommand
DELETE /api/teacher/exams/{id}/questions/{qid}         → RemoveQuestionCommand
PUT    /api/teacher/exams/{id}/publish                 → PublishExamCommand
GET    /api/teacher/exams/{taId}                       → GetMyExamsQuery
GET    /api/teacher/grading/pending                    → GetPendingGradingQuery
GET    /api/teacher/grading/exam/{studentExamId}       → GetStudentAnswersForReviewQuery
POST   /api/teacher/grading/answers/{answerId}         → GradeStudentAnswerCommand

// Student — Assignments
GET    /api/student/assignments/{subjectId}            → GetMyAssignmentsForSubjectQuery
POST   /api/student/assignments/{id}/submit            → SubmitAssignmentCommand (multipart/form-data)

// Student — Exams
GET    /api/student/exams/{subjectId}                  → GetOpenExamsForSubjectQuery
POST   /api/student/exams/{id}/start                   → StartExamAttemptCommand
POST   /api/student/attempts/{attemptId}/save-answer   → SaveAnswerCommand
POST   /api/student/attempts/{attemptId}/submit        → SubmitExamCommand
GET    /api/student/attempts/{attemptId}/result        → GetMyExamResultQuery

// Student — Performance
GET    /api/student/performance                        → GetMyPerformanceQuery

// Admin
GET    /api/admin/performance/class/{classId}/{subjectId} → GetClassPerformanceReportQuery
```

---

## 6. Database Migration

```
Migration name: Phase3_AssessmentAndGrading

New tables: assignments, submissions, exams, questions, question_options,
            student_exams, student_answers, student_performances

Key indexes:
- student_exams(student_user_id, exam_id) UNIQUE
- student_answers(student_exam_id, question_id) UNIQUE  
- student_performances(student_user_id, subject_id, class_id, academic_year) UNIQUE
- exams(teaching_assignment_id, open_at, close_at) — open exam lookup
- student_answers(grading_status) WHERE grading_status = 'PendingReview' — teacher grading queue
```

---

## 7. Angular Frontend

### Module Structure

```
features/
  assessment/
    pages/
      teacher/
        assignment-creator/         ← form: title, instructions, due date, max marks
        assignment-submissions/     ← list submissions, grade each, download file
        exam-creator/               ← multi-step: exam meta → add questions
        question-editor/            ← question type selector + dynamic form per type
        grading-dashboard/          ← pending grading overview
        exam-grader/                ← review student answers one by one
      student/
        my-assignments/             ← list by subject, submit
        assignment-submit/          ← text area + file upload
        my-exams/                   ← list open exams
        exam-taking/                ← timer + question navigator + answer forms
        exam-result/                ← score, answer breakdown
        my-performance/             ← performance per subject, charts
    components/
      exam-timer/                   ← countdown component, emits autoSubmit event
      question-renderer/            ← renders question by type dynamically
      answer-input/                 ← MCQ radio / T-F toggle / text input / file upload
      score-badge/
      performance-chart/            ← line/bar chart using ng2-charts or Chart.js
    services/
      assessment.service.ts
      exam.service.ts
      performance.service.ts
    store/
      exam-attempt.state.ts         ← active exam in progress: answers, timer, questionIndex
      exam-attempt.actions.ts
      exam-attempt.effects.ts       ← auto-save answer to API every 30 seconds
      exam-attempt.selectors.ts
```

### Exam-Taking Component — Critical UX Rules

- Timer component counts down from `SecondsRemaining` using `setInterval`
- At 5 minutes remaining: warning toast displayed
- At 0: `autoSubmit()` emits, effect calls `SubmitExamCommand`
- Navigator shows question number, answered status (grey = unanswered, green = answered)
- Auto-save effect: on each `SaveAnswer` action, debounce 2 seconds then call API
- On page refresh: `StartExamAttemptCommand` returns existing attempt with correct remaining time
- File upload answers: upload directly to API (multipart); show upload progress bar

### Angular Models (Assessment)

```typescript
export interface ExamAttempt { studentExamId: number; examTitle: string; expiresAt: string; secondsRemaining: number; questions: Question[]; savedAnswers: SavedAnswer[]; }
export interface Question { questionId: number; type: QuestionType; text: string; marks: number; order: number; options?: QuestionOption[]; }
export interface SavedAnswer { questionId: number; answerText?: string; selectedOptionId?: string; }
export type QuestionType = 'MCQ' | 'TrueFalse' | 'ShortAnswer' | 'Essay' | 'FillInBlank' | 'FileUpload';
export interface ExamResult { finalScore: number; totalMarks: number; percentage: number; hasPendingManualGrading: boolean; answers: AnswerResult[]; }
export interface SubjectPerformance { subjectId: number; subjectName: string; averageExamScore: number; averageAssignmentScore: number; }
```

---

## 8. Definition of Done

- [ ] MCQ, T/F, FIB answers auto-graded correctly on submission (verified by unit tests)
- [ ] Short Answer, Essay, File Upload correctly flagged as `PendingReview`
- [ ] Exam timer enforced server-side: expired attempts auto-submitted by background job
- [ ] Teacher can grade pending answers one by one and finalize score
- [ ] `StudentPerformance` recalculated asynchronously via RabbitMQ after every grading event
- [ ] Azure Blob Storage uploads and signed URL download work end-to-end
- [ ] Student cannot start the same exam twice; returning mid-exam restores remaining time
- [ ] Angular exam-taking UI: timer counts down, auto-saves every 30s, auto-submits at 0
- [ ] All unit tests for `AutoGradingService` cover all 3 auto-graded types including edge cases
- [ ] Integration test: full exam lifecycle (create → publish → student takes → submit → auto-grade → view result)
- [ ] RabbitMQ consumer verified with retry and dead-letter behavior

---

## 9. Risks

| Risk | Mitigation |
|---|---|
| RabbitMQ unavailable in dev | Use MassTransit InMemory transport in Development environment |
| Azure Blob Storage not configured | Use local file system (`IFileStorageService` local impl) in Development |
| Concurrent answer saves from same student | Last-write-wins on `StudentAnswer` update; idempotent save endpoint |
| Timer drift between client and server | Server is authoritative; `ExpiresAt` is server-stored; recalculate `SecondsRemaining` on each `StartExam` call |
| FIB grading fails on minor typos | Consider Levenshtein distance for near-matches as a teacher-configurable option (implement as nullable tolerance field on Question) |
