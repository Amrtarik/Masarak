import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';

import { AuthStateService } from '../../../core/services/auth-state-service';
import { environment } from '../../../../environments/environment';
import {
  SaveStudentExamAnswersRequest,
  SaveStudentExamAnswersResponse,
  StartStudentExamResponse,
  StudentAssignment,
  StudentAssignmentGroup,
  StudentAssignmentSubmission,
  StudentAttendanceRecord,
  StudentAttendanceSummary,
  StudentClass,
  StudentContentGroup,
  StudentContentItem,
  StudentCourse,
  StudentDashboardData,
  StudentEntityId,
  StudentExam,
  StudentExamAnswer,
  StudentExamChoice,
  StudentExamGroup,
  StudentExamQuestion,
  StudentExamResult,
  StudentGrade,
  StudentPerformance,
  StudentProfile,
  StudentScheduleSession,
  StudentSubjectAttendance,
  SubmitAssignmentRequest,
  SubmitStudentExamResponse,
} from '../models';

type StudentAcademicYear = number;

interface StudentApiErrorBody {
  detail?: string;
  error?: string;
  errors?: Record<string, string[]>;
  message?: string;
  title?: string;
}

interface StudentEnrollmentDto {
  classId?: number;
  className?: string | null;
  gradeName?: string | null;
  academicYear?: number;
}

interface WeeklyScheduleDto {
  weekStart?: string;
  weekEnd?: string;
  sessions?: SessionDto[] | null;
}

interface SessionDto {
  sessionId?: number;
  title?: string | null;
  description?: string | null;
  scheduledAt?: string;
  durationMinutes?: number;
  endsAt?: string;
  embedUrl?: string | null;
  status?: string;
  subjectName?: string | null;
  className?: string | null;
  teacherName?: string | null;
}

interface SubjectAttendanceDto {
  subjectId?: number;
  subjectName?: string | null;
  totalSessions?: number;
  presentCount?: number;
  absentCount?: number;
  excusedCount?: number;
  attendancePercentage?: number;
}

interface ContentItemDto {
  contentItemId?: number;
  type?: string;
  sourceType?: string;
  title?: string | null;
  description?: string | null;
  resourceUrl?: string | null;
  fileSizeBytes?: number | null;
  createdAt?: string;
  isActive?: boolean;
}

interface AssignmentDto {
  assignmentId?: number;
  title?: string | null;
  dueDate?: string;
  maxScore?: number;
  status?: string;
  subjectName?: string | null;
  className?: string | null;
  submissionCount?: number;
}

interface SubmissionDto {
  submissionId?: number;
  assignmentId?: number;
  status?: string;
  submittedAt?: string;
  score?: number | null;
  feedback?: string | null;
  fileUrl?: string | null;
}

interface ExamDto {
  examId?: number;
  title?: string | null;
  startTime?: string;
  endTime?: string;
  durationMinutes?: number;
  totalMarks?: number;
  status?: string;
  questionCount?: number;
}

interface QuestionOptionDto {
  label?: string | null;
  text?: string | null;
}

interface QuestionDto {
  questionId?: number;
  type?: string;
  text?: string | null;
  marks?: number;
  options?: QuestionOptionDto[] | null;
}

interface SavedAnswerDto {
  questionId?: number;
  answerText?: string | null;
  selectedOptionId?: string | null;
}

interface ExamAttemptDto {
  studentExamId?: number;
  examId?: number;
  examTitle?: string | null;
  expiresAt?: string;
  secondsRemaining?: number;
  questions?: QuestionDto[] | null;
  savedAnswers?: SavedAnswerDto[] | null;
}

interface ExamResultDto {
  studentExamId?: number;
  finalScore?: number;
  totalMarks?: number;
  percentage?: number;
  hasPendingManualGrading?: boolean;
}

interface SubjectPerformanceDto {
  subjectId?: number;
  subjectName?: string | null;
  averageExamScore?: number;
  averageAssignmentScore?: number;
  totalExamsTaken?: number;
  totalAssignmentsSubmitted?: number;
}

@Injectable({ providedIn: 'root' })
export class StudentService {
  private readonly http = inject(HttpClient);
  private readonly authState = inject(AuthStateService);
  private readonly baseUrl = `${environment.apiUrl}/student`;

  getProfile(academicYear?: StudentAcademicYear): Observable<StudentProfile> {
    return this.getMyClass(academicYear).pipe(map((classInfo) => this.createProfile(classInfo)));
  }

  getCourses(): Observable<StudentCourse[]> {
    return this.http.get<unknown>(`${this.baseUrl}/courses`).pipe(
      map((payload) => this.normalizeArray(payload).map((item) => this.mapCourse(item))),
      map((courses) => courses.filter(this.isDefined)),
    );
  }

  getGrades(): Observable<StudentGrade[]> {
    return this.http.get<unknown>(`${this.baseUrl}/grades`).pipe(
      map((payload) => this.normalizeArray(payload).map((item) => this.mapGrade(item))),
      map((grades) => grades.filter(this.isDefined)),
    );
  }

  getMyClass(academicYear?: StudentAcademicYear): Observable<StudentClass> {
    return this.http
      .get<StudentEnrollmentDto>(`${this.baseUrl}/my-class`, {
        params: this.buildParams({ academicYear }),
      })
      .pipe(map((dto) => this.mapClass(dto)));
  }

  getSchedule(
    academicYear?: StudentAcademicYear,
    weekStart?: string | Date,
  ): Observable<StudentScheduleSession[]> {
    return this.http
      .get<WeeklyScheduleDto>(`${this.baseUrl}/schedule`, {
        params: this.buildParams({
          academicYear,
          weekStart: this.toDateQueryValue(weekStart),
        }),
      })
      .pipe(map((dto) => (dto.sessions ?? []).map((session) => this.mapSession(session))));
  }

  getContent(subjectId: StudentEntityId): Observable<StudentContentItem[]> {
    return this.http
      .get<ContentItemDto[]>(`${this.baseUrl}/content/${encodeURIComponent(String(subjectId))}`)
      .pipe(map((items) => items.map((item) => this.mapContentItem(item, subjectId))));
  }

  getAttendance(academicYear?: StudentAcademicYear): Observable<StudentAttendanceSummary> {
    return this.http
      .get<SubjectAttendanceDto[]>(`${this.baseUrl}/attendance`, {
        params: this.buildParams({ academicYear }),
      })
      .pipe(map((rows) => this.mapAttendanceSummary(rows)));
  }

  getPerformance(academicYear?: StudentAcademicYear): Observable<StudentPerformance[]> {
    return this.http
      .get<SubjectPerformanceDto[]>(`${this.baseUrl}/assessment/performance`, {
        params: this.buildParams({
          academicYear: academicYear === undefined ? undefined : String(academicYear),
        }),
      })
      .pipe(map((rows) => rows.map((row) => this.mapPerformance(row))));
  }

  getAssignments(subjectId: StudentEntityId): Observable<StudentAssignment[]> {
    return this.http
      .get<AssignmentDto[]>(
        `${this.baseUrl}/assessment/subjects/${encodeURIComponent(String(subjectId))}/assignments`,
      )
      .pipe(map((items) => items.map((item) => this.mapAssignment(item, subjectId))));
  }

  submitAssignment(
    assignmentId: StudentEntityId,
    request: SubmitAssignmentRequest,
  ): Observable<StudentAssignmentSubmission> {
    return this.http
      .post<SubmissionDto>(
        `${this.baseUrl}/assessment/assignments/${encodeURIComponent(String(assignmentId))}/submit`,
        this.createAssignmentFormData(request),
      )
      .pipe(map((submission) => this.mapSubmission(submission, assignmentId)));
  }

  markAssignmentSubmitted(assignmentId: StudentEntityId): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/assignments/${encodeURIComponent(String(assignmentId))}/submit`,
      null,
    );
  }

  getExams(subjectId: StudentEntityId): Observable<StudentExam[]> {
    return this.http
      .get<ExamDto[]>(
        `${this.baseUrl}/assessment/subjects/${encodeURIComponent(String(subjectId))}/exams`,
      )
      .pipe(map((items) => items.map((item) => this.mapExam(item, subjectId))));
  }

  getStudentExams(): Observable<StudentExam[]> {
    return this.http.get<unknown>(`${this.baseUrl}/exams`).pipe(
      map((payload) => this.normalizeArray(payload).map((item) => this.mapExamFromUnknown(item))),
      map((exams) => exams.filter(this.isDefined)),
    );
  }

  startExam(examId: StudentEntityId): Observable<StartStudentExamResponse> {
    return this.http
      .post<ExamAttemptDto>(
        `${this.baseUrl}/assessment/exams/${encodeURIComponent(String(examId))}/start`,
        null,
      )
      .pipe(map((attempt) => this.mapExamAttempt(attempt, examId)));
  }

  saveExamAnswers(
    studentExamId: StudentEntityId,
    request: SaveStudentExamAnswersRequest,
  ): Observable<SaveStudentExamAnswersResponse> {
    const answerRequests = request.answers
      .filter((answer) => this.hasAnswerPayload(answer))
      .map((answer) =>
        this.http.post<void>(
          `${this.baseUrl}/assessment/student-exams/${encodeURIComponent(
            String(studentExamId),
          )}/answers`,
          this.createExamAnswerFormData(answer),
        ),
      );

    if (answerRequests.length === 0) {
      return of({
        studentExamId,
        savedAt: new Date().toISOString(),
        message: 'No answers to save.',
      });
    }

    return forkJoin(answerRequests).pipe(
      map(() => ({
        studentExamId,
        savedAt: new Date().toISOString(),
        message: 'Answers saved.',
      })),
    );
  }

  submitExam(studentExamId: StudentEntityId): Observable<SubmitStudentExamResponse> {
    return this.http
      .post<ExamResultDto>(
        `${this.baseUrl}/assessment/student-exams/${encodeURIComponent(
          String(studentExamId),
        )}/submit`,
        null,
      )
      .pipe(map((result) => this.mapExamResult(result, studentExamId)));
  }

  getExamResult(studentExamId: StudentEntityId): Observable<StudentExamResult> {
    return this.http
      .get<ExamResultDto>(
        `${this.baseUrl}/assessment/student-exams/${encodeURIComponent(
          String(studentExamId),
        )}/result`,
      )
      .pipe(map((result) => this.mapExamResult(result, studentExamId)));
  }

  joinSession(sessionId: StudentEntityId): Observable<StudentAttendanceRecord> {
    return this.http
      .post<unknown>(
        `${this.baseUrl}/sessions/${encodeURIComponent(String(sessionId))}/join`,
        null,
      )
      .pipe(map((payload) => this.mapAttendanceRecord(payload, sessionId)));
  }

  getDashboardData(academicYear?: StudentAcademicYear): Observable<StudentDashboardData> {
    return this.getMyClass(academicYear).pipe(
      switchMap((classInfo) => {
        const selectedAcademicYear = classInfo.academicYear ?? academicYear;

        return forkJoin({
          profile: of(this.createProfile(classInfo)),
          classInfo: of(classInfo),
          courses: this.getCourses(),
          schedule: this.getSchedule(selectedAcademicYear),
          attendance: this.getAttendance(selectedAcademicYear),
          performance: this.getPerformance(selectedAcademicYear),
        });
      }),
    );
  }

  getContentLibrary(): Observable<StudentContentGroup[]> {
    return this.getCourses().pipe(
      switchMap((courses) => {
        if (courses.length === 0) {
          return of([]);
        }

        return forkJoin(
          courses.map((course) =>
            this.getContent(course.subjectId).pipe(map((items) => ({ subject: course, items }))),
          ),
        );
      }),
    );
  }

  getAssignmentGroups(): Observable<StudentAssignmentGroup[]> {
    return this.getCourses().pipe(
      switchMap((courses) => {
        if (courses.length === 0) {
          return of([]);
        }

        return forkJoin(
          courses.map((course) =>
            this.getAssignments(course.subjectId).pipe(
              map((assignments) => ({ subject: course, assignments })),
            ),
          ),
        );
      }),
    );
  }

  getExamGroups(): Observable<StudentExamGroup[]> {
    return this.getCourses().pipe(
      switchMap((courses) => {
        if (courses.length === 0) {
          return of([]);
        }

        return forkJoin(
          courses.map((course) =>
            this.getExams(course.subjectId).pipe(map((exams) => ({ subject: course, exams }))),
          ),
        );
      }),
    );
  }

  resolveErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'Unable to reach the server. Please check your connection and try again.';
      }

      const apiMessage = this.readApiErrorMessage(error.error);

      if (apiMessage) {
        return apiMessage;
      }

      return `Request failed with status ${error.status}. Please try again.`;
    }

    return 'Something went wrong. Please try again.';
  }

  private mapClass(dto: StudentEnrollmentDto): StudentClass {
    return {
      classId: dto.classId,
      className: dto.className ?? 'Not assigned',
      gradeName: dto.gradeName ?? undefined,
      academicYear: dto.academicYear,
    };
  }

  private createProfile(classInfo: StudentClass): StudentProfile {
    const user = this.authState.user();

    return {
      studentId: user?.userId,
      fullName: user?.fullName ?? 'Student',
      email: user?.email ?? '',
      academicStatus: user?.isActive === false ? 'Inactive' : 'Active',
      gradeName: classInfo.gradeName ?? classInfo.className,
    };
  }

  private mapCourse(value: unknown): StudentCourse | null {
    const record = this.asRecord(value);

    if (!record) {
      return null;
    }

    const subjectId = this.readEntityId(record, ['subjectId', 'id']);

    if (subjectId === undefined) {
      return null;
    }

    const subjectName = this.readString(record, ['subjectName', 'name', 'title']) ?? 'Untitled';
    const subjectArabicName =
      this.readString(record, ['subjectArabicName', 'nameAr', 'arabicName']) ?? subjectName;

    return {
      subjectId,
      subjectName,
      subjectArabicName,
      description: this.readString(record, ['description']),
      teacherName: this.readString(record, ['teacherName', 'teacherFullName']),
      gradeName: this.readString(record, ['gradeName']),
    };
  }

  private mapGrade(value: unknown): StudentGrade | null {
    const record = this.asRecord(value);

    if (!record) {
      return null;
    }

    const finalGrade =
      this.readNumber(record, ['finalGrade', 'finalScore', 'score', 'percentage']) ?? 0;
    const subjectName = this.readString(record, ['subjectName', 'name', 'title']) ?? 'Subject';

    return {
      subjectId: this.readEntityId(record, ['subjectId']),
      subjectName,
      gradeLetter:
        this.readString(record, ['gradeLetter', 'letterGrade']) ?? this.toGradeLetter(finalGrade),
      finalGrade,
      termName: this.readString(record, ['termName', 'term']),
      academicYear: this.readString(record, ['academicYear']),
      examAverage: this.readNumber(record, ['examAverage', 'averageExamScore']),
      assignmentAverage: this.readNumber(record, [
        'assignmentAverage',
        'averageAssignmentScore',
      ]),
    };
  }

  private mapSession(session: SessionDto): StudentScheduleSession {
    return {
      sessionId: session.sessionId,
      sessionTitle: session.title ?? 'Session',
      sessionDate: session.scheduledAt ?? '',
      duration: session.durationMinutes ?? '',
      endsAt: session.endsAt,
      status: session.status,
      subjectName: session.subjectName ?? undefined,
      className: session.className ?? undefined,
      teacherName: session.teacherName ?? undefined,
      meetingUrl: session.embedUrl ?? undefined,
      description: session.description ?? undefined,
    };
  }

  private mapContentItem(item: ContentItemDto, subjectId: StudentEntityId): StudentContentItem {
    return {
      contentId: item.contentItemId ?? 0,
      subjectId,
      title: item.title ?? 'Untitled resource',
      contentType: item.type ?? 'Resource',
      sourceType: item.sourceType,
      description: item.description ?? undefined,
      fileUrl: item.resourceUrl ?? undefined,
      url: item.resourceUrl ?? undefined,
      fileSizeBytes: item.fileSizeBytes,
      createdAt: item.createdAt,
      isActive: item.isActive,
    };
  }

  private mapAttendanceSummary(rows: SubjectAttendanceDto[]): StudentAttendanceSummary {
    const subjects = rows.map((row) => this.mapSubjectAttendance(row));
    const presentCount = subjects.reduce((total, row) => total + row.presentCount, 0);
    const absentCount = subjects.reduce((total, row) => total + row.absentCount, 0);
    const excusedCount = subjects.reduce((total, row) => total + row.excusedCount, 0);
    const totalSessions = subjects.reduce((total, row) => total + row.totalSessions, 0);
    const attendanceRate =
      totalSessions > 0 ? Math.round((presentCount / totalSessions) * 100) : 0;

    return {
      attendanceRate,
      presentCount,
      absentCount,
      lateCount: 0,
      excusedCount,
      totalSessions,
      subjects,
      records: [],
    };
  }

  private mapSubjectAttendance(row: SubjectAttendanceDto): StudentSubjectAttendance {
    return {
      subjectId: row.subjectId ?? 0,
      subjectName: row.subjectName ?? 'Subject',
      totalSessions: row.totalSessions ?? 0,
      presentCount: row.presentCount ?? 0,
      absentCount: row.absentCount ?? 0,
      excusedCount: row.excusedCount ?? 0,
      attendancePercentage: row.attendancePercentage ?? 0,
    };
  }

  private mapPerformance(row: SubjectPerformanceDto): StudentPerformance {
    const avgExam = row.averageExamScore ?? 0;
    const avgAssignment = row.averageAssignmentScore ?? 0;
    const finalGrade =
      avgExam > 0 && avgAssignment > 0 ? (avgExam + avgAssignment) / 2 : avgExam || avgAssignment;

    return {
      subjectId: row.subjectId,
      subjectName: row.subjectName ?? 'Subject',
      finalGrade,
      gradeLetter: this.toGradeLetter(finalGrade),
      avgExam,
      avgAssignment,
    };
  }

  private mapAssignment(item: AssignmentDto, subjectId: StudentEntityId): StudentAssignment {
    return {
      assignmentId: item.assignmentId ?? 0,
      subjectId,
      title: item.title ?? 'Untitled assignment',
      dueDate: item.dueDate,
      maxScore: item.maxScore,
      status: item.status,
      description: item.className ? `Class: ${item.className}` : undefined,
    };
  }

  private mapSubmission(
    submission: SubmissionDto,
    assignmentId: StudentEntityId,
  ): StudentAssignmentSubmission {
    return {
      submissionId: submission.submissionId,
      assignmentId: submission.assignmentId ?? assignmentId,
      submittedAt: submission.submittedAt ?? new Date().toISOString(),
      status: submission.status ?? 'Submitted',
      score: submission.score,
      feedback: submission.feedback,
      fileUrl: submission.fileUrl,
    };
  }

  private mapExam(item: ExamDto, subjectId?: StudentEntityId): StudentExam {
    return {
      examId: item.examId ?? 0,
      subjectId,
      title: item.title ?? 'Untitled exam',
      startsAt: item.startTime,
      endsAt: item.endTime,
      durationMinutes: item.durationMinutes,
      questionCount: item.questionCount,
      maxScore: item.totalMarks,
      status: item.status,
    };
  }

  private mapExamFromUnknown(value: unknown): StudentExam | null {
    const record = this.asRecord(value);

    if (!record) {
      return null;
    }

    const examId = this.readEntityId(record, ['examId', 'id']);

    if (examId === undefined) {
      return null;
    }

    return {
      examId,
      subjectId: this.readEntityId(record, ['subjectId']),
      title: this.readString(record, ['title', 'examTitle']) ?? 'Untitled exam',
      description: this.readString(record, ['description', 'instructions']),
      startsAt: this.readString(record, ['startTime', 'startsAt']),
      endsAt: this.readString(record, ['endTime', 'endsAt']),
      durationMinutes: this.readNumber(record, ['durationMinutes']),
      questionCount: this.readNumber(record, ['questionCount']),
      maxScore: this.readNumber(record, ['totalMarks', 'maxScore']),
      status: this.readString(record, ['status']),
    };
  }

  private mapExamAttempt(
    attempt: ExamAttemptDto,
    fallbackExamId: StudentEntityId,
  ): StartStudentExamResponse {
    return {
      studentExamId: attempt.studentExamId ?? 0,
      examId: attempt.examId ?? fallbackExamId,
      title: attempt.examTitle ?? undefined,
      startedAt: new Date().toISOString(),
      expiresAt: attempt.expiresAt,
      secondsRemaining: attempt.secondsRemaining,
      questions: (attempt.questions ?? []).map((question) => this.mapQuestion(question)),
      savedAnswers: (attempt.savedAnswers ?? []).map((answer) => this.mapSavedAnswer(answer)),
    };
  }

  private mapQuestion(question: QuestionDto): StudentExamQuestion {
    const questionId = question.questionId ?? 0;

    return {
      questionId,
      text: question.text ?? 'Question',
      questionType: question.type ?? 'ShortAnswer',
      points: question.marks,
      choices: (question.options ?? []).map((option, index) =>
        this.mapQuestionChoice(option, questionId, index),
      ),
    };
  }

  private mapQuestionChoice(
    option: QuestionOptionDto,
    questionId: StudentEntityId,
    index: number,
  ): StudentExamChoice {
    const label = option.label ?? String(index + 1);
    const text = option.text ?? option.label ?? `Option ${index + 1}`;

    return {
      choiceId: label,
      label,
      text,
    };
  }

  private mapSavedAnswer(answer: SavedAnswerDto): StudentExamAnswer {
    const selectedOptionId = answer.selectedOptionId ?? undefined;

    return {
      questionId: answer.questionId ?? 0,
      answerText: answer.answerText ?? undefined,
      selectedOptionId,
      selectedChoiceIds: selectedOptionId ? [selectedOptionId] : undefined,
    };
  }

  private mapExamResult(
    result: ExamResultDto,
    studentExamId: StudentEntityId,
  ): StudentExamResult {
    const score = result.finalScore ?? 0;
    const maxScore = result.totalMarks ?? 0;
    const percentage =
      result.percentage ?? (maxScore > 0 ? Math.round((score / maxScore) * 100) : 0);

    return {
      studentExamId: result.studentExamId ?? studentExamId,
      score,
      maxScore,
      percentage,
      gradeLetter: this.toGradeLetter(percentage),
      hasPendingManualGrading: result.hasPendingManualGrading,
    };
  }

  private mapAttendanceRecord(
    value: unknown,
    fallbackSessionId: StudentEntityId,
  ): StudentAttendanceRecord {
    const record = this.asRecord(value);

    return {
      attendanceId: record ? this.readEntityId(record, ['attendanceId']) : undefined,
      sessionId: record ? this.readEntityId(record, ['sessionId']) : fallbackSessionId,
      sessionTitle: record ? this.readString(record, ['sessionTitle']) : undefined,
      sessionDate: record ? this.readString(record, ['joinedAt']) ?? new Date().toISOString() : '',
      status: record ? this.readString(record, ['status']) ?? 'Present' : 'Present',
      notes: record ? this.readString(record, ['teacherNote', 'notes']) : undefined,
    };
  }

  private createAssignmentFormData(request: SubmitAssignmentRequest): FormData {
    const formData = new FormData();
    const textContent = request.textContent?.trim();

    if (textContent) {
      formData.append('TextContent', textContent);
    }

    if (request.file) {
      formData.append('file', request.file);
    }

    return formData;
  }

  private createExamAnswerFormData(answer: StudentExamAnswer): FormData {
    const formData = new FormData();
    const selectedOptionId = this.getSelectedOptionId(answer);

    formData.append('QuestionId', String(answer.questionId));

    if (answer.answerText?.trim()) {
      formData.append('AnswerText', answer.answerText.trim());
    }

    if (selectedOptionId) {
      formData.append('SelectedOptionId', selectedOptionId);
    }

    if (answer.file) {
      formData.append('file', answer.file);
    }

    return formData;
  }

  private hasAnswerPayload(answer: StudentExamAnswer): boolean {
    return Boolean(answer.answerText?.trim() || this.getSelectedOptionId(answer) || answer.file);
  }

  private getSelectedOptionId(answer: StudentExamAnswer): string | undefined {
    if (answer.selectedOptionId) {
      return answer.selectedOptionId;
    }

    const selectedChoiceIds = answer.selectedChoiceIds ?? [];
    return selectedChoiceIds.length > 0 ? selectedChoiceIds.map(String).join(',') : undefined;
  }

  private buildParams(
    values: Record<string, string | number | boolean | undefined | null>,
  ): HttpParams {
    let params = new HttpParams();

    Object.entries(values).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return params;
  }

  private toDateQueryValue(value?: string | Date): string | undefined {
    if (value instanceof Date) {
      return value.toISOString();
    }

    return value;
  }

  private normalizeArray(value: unknown): unknown[] {
    if (Array.isArray(value)) {
      return value;
    }

    const record = this.asRecord(value);

    if (record) {
      for (const key of ['items', 'data', 'value', 'results']) {
        const candidate = record[key];

        if (Array.isArray(candidate)) {
          return candidate;
        }
      }
    }

    return [];
  }

  private asRecord(value: unknown): Record<string, unknown> | null {
    return typeof value === 'object' && value !== null && !Array.isArray(value)
      ? (value as Record<string, unknown>)
      : null;
  }

  private readString(record: Record<string, unknown>, keys: string[]): string | undefined {
    for (const key of keys) {
      const value = record[key];

      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }

      if (typeof value === 'number' && Number.isFinite(value)) {
        return String(value);
      }
    }

    return undefined;
  }

  private readNumber(record: Record<string, unknown>, keys: string[]): number | undefined {
    for (const key of keys) {
      const value = record[key];

      if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
      }

      if (typeof value === 'string' && value.trim().length > 0) {
        const parsed = Number(value);

        if (Number.isFinite(parsed)) {
          return parsed;
        }
      }
    }

    return undefined;
  }

  private readEntityId(
    record: Record<string, unknown>,
    keys: string[],
  ): StudentEntityId | undefined {
    for (const key of keys) {
      const value = record[key];

      if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
      }

      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }

    return undefined;
  }

  private toGradeLetter(score: number): string {
    if (score >= 90) {
      return 'A';
    }

    if (score >= 80) {
      return 'B';
    }

    if (score >= 70) {
      return 'C';
    }

    if (score >= 60) {
      return 'D';
    }

    return 'F';
  }

  private readApiErrorMessage(value: unknown): string | null {
    if (typeof value === 'string') {
      return value.trim().length > 0 ? value : null;
    }

    if (!this.isApiErrorBody(value)) {
      return null;
    }

    const validationMessage = this.readValidationErrors(value.errors);
    return value.message ?? value.detail ?? value.title ?? value.error ?? validationMessage;
  }

  private readValidationErrors(errors?: Record<string, string[]>): string | null {
    if (!errors) {
      return null;
    }

    const messages = Object.values(errors).flat().filter((message) => message.trim().length > 0);
    return messages.length > 0 ? messages.join(' ') : null;
  }

  private isApiErrorBody(value: unknown): value is StudentApiErrorBody {
    if (typeof value !== 'object' || value === null) {
      return false;
    }

    const record = value as Record<string, unknown>;
    const errors = record['errors'];

    return (
      this.isOptionalString(record['message']) &&
      this.isOptionalString(record['title']) &&
      this.isOptionalString(record['detail']) &&
      this.isOptionalString(record['error']) &&
      (errors === undefined || this.isValidationErrorMap(errors))
    );
  }

  private isOptionalString(value: unknown): boolean {
    return value === undefined || typeof value === 'string';
  }

  private isValidationErrorMap(value: unknown): value is Record<string, string[]> {
    const record = this.asRecord(value);

    if (!record) {
      return false;
    }

    return Object.values(record).every(
      (messages) => Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
    );
  }

  private isDefined<T>(value: T | null | undefined): value is T {
    return value !== null && value !== undefined;
  }
}
