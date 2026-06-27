// ── Grade Stage Enum (matches backend) ──────────────────────────────────────
export enum GradeStage {
  Primary = 0,      // ابتدائي
  Preparatory = 1,  // إعدادي
  Secondary = 2,    // ثانوي
}

export interface GradeDto {
  gradeId: number;
  name: string;
  nameAr?: string;
  stage: GradeStage;
  order: number;
  isActive: boolean;
}

export interface CreateGradeRequest {
  name: string;
  nameAr?: string;
  stage: GradeStage;  // REQUIRED by backend
  order: number;       // REQUIRED by backend
}

export interface UpdateGradeRequest {
  name: string;
  nameAr?: string;
  isActive: boolean;
}

export interface SubjectDto {
  subjectId: number;
  gradeId: number;
  name: string;
  nameAr?: string;
  code: string;    // REQUIRED by backend
  isActive: boolean;
}

export interface CreateSubjectRequest {
  gradeId: number;
  name: string;
  nameAr?: string;
  code: string;    // REQUIRED by backend [Required, MaxLength(20)]
}

export interface UpdateSubjectRequest {
  name: string;
  nameAr?: string;
  isActive: boolean;
}

export interface ClassDto {
  classId: number;
  gradeId: number;
  gradeName: string;
  name: string;
  maxCapacity: number;
  academicYear: number;   // int on backend
  enrollmentCount: number;
  isActive: boolean;
}

export interface CreateClassRequest {
  gradeId: number;
  name: string;
  maxCapacity: number;  // REQUIRED [Range(1,500)], default 30
  academicYear: number; // REQUIRED int [Range(2020,2100)]
}

export interface UpdateClassRequest {
  name: string;
  maxCapacity: number;
  isActive: boolean;
}

export interface StudentInClassDto {
  studentId: number;
  fullName: string;  // backend returns 'FullName' not 'studentName'
  email: string;
}

export interface EnrollStudentRequest {
  studentId: number;
  classId: number;
  academicYear: number; // REQUIRED by backend [Range(2020,2100)]
}

export interface TeachingAssignmentDto {
  id: number;        // backend returns 'Id' not 'assignmentId'
  teacherId: number;
  teacherName: string;
  className: string;
  subjectName: string;
  academicYear: number;
  isActive: boolean;
}

export interface AssignTeacherRequest {
  classId: number;
  subjectId: number;
  teacherId: number;
  academicYear: number; // REQUIRED int by backend [Range(2020,2100)]
}

export interface ClassPerformanceReportDto {
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  academicYear: string;
  averageScore: number;
  totalStudents: number;
  passedStudents: number;
  highestScore: number;
  lowestScore: number;
}

// ─── Teacher Directory ─────────────────────────────────────────────────────────

export interface TeacherCourseDto {
  assignmentId: number;
  className: string;
  subjectName: string;
  academicYear: number;
}

export interface TeacherDto {
  teacherId: number;
  userId: number;
  fullName: string;
  email: string;
  specialization: string | null;
  hiringDate: string;
  bio: string | null;
  courses: TeacherCourseDto[];
}
