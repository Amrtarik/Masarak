import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GradeDto,
  CreateGradeRequest,
  UpdateGradeRequest,
  SubjectDto,
  CreateSubjectRequest,
  UpdateSubjectRequest,
  ClassDto,
  CreateClassRequest,
  UpdateClassRequest,
  StudentInClassDto,
  TeachingAssignmentDto,
  AssignTeacherRequest,
  EnrollStudentRequest,
  TeacherDto,
} from '../../models/academic.model';

@Injectable({ providedIn: 'root' })
export class AcademicApiService {
  private readonly http = inject(HttpClient);
  private readonly adminBase = `${environment.apiUrl}/admin`;

  // ── Grades ──────────────────────────────────────────────────────────
  getGrades(): Observable<GradeDto[]> {
    return this.http.get<GradeDto[]>(`${this.adminBase}/grades`);
  }

  createGrade(req: CreateGradeRequest): Observable<GradeDto> {
    return this.http.post<GradeDto>(`${this.adminBase}/grades`, req);
  }

  updateGrade(id: number, req: UpdateGradeRequest): Observable<GradeDto> {
    return this.http.put<GradeDto>(`${this.adminBase}/grades/${id}`, req);
  }

  // ── Subjects ────────────────────────────────────────────────────────
  getSubjectsByGrade(gradeId: number): Observable<SubjectDto[]> {
    return this.http.get<SubjectDto[]>(`${this.adminBase}/grades/${gradeId}/subjects`);
  }

  createSubject(req: CreateSubjectRequest): Observable<SubjectDto> {
    return this.http.post<SubjectDto>(`${this.adminBase}/subjects`, req);
  }

  updateSubject(id: number, req: UpdateSubjectRequest): Observable<SubjectDto> {
    return this.http.put<SubjectDto>(`${this.adminBase}/subjects/${id}`, req);
  }

  // ── Classes ─────────────────────────────────────────────────────────
  getClassesByGrade(gradeId: number, academicYear: string | number): Observable<ClassDto[]> {
    let params = new HttpParams().set('academicYear', academicYear.toString());
    return this.http.get<ClassDto[]>(`${this.adminBase}/grades/${gradeId}/classes`, { params });
  }

  createClass(req: CreateClassRequest): Observable<ClassDto> {
    return this.http.post<ClassDto>(`${this.adminBase}/classes`, req);
  }

  updateClass(id: number, req: UpdateClassRequest): Observable<ClassDto> {
    return this.http.put<ClassDto>(`${this.adminBase}/classes/${id}`, req);
  }

  getClassRoster(classId: number): Observable<StudentInClassDto[]> {
    return this.http.get<StudentInClassDto[]>(`${this.adminBase}/classes/${classId}/roster`);
  }

  // ── Teaching Assignments ────────────────────────────────────────────
  getAssignmentsForClass(classId: number, academicYear: string | number): Observable<TeachingAssignmentDto[]> {
    let params = new HttpParams().set('academicYear', academicYear.toString());
    return this.http.get<TeachingAssignmentDto[]>(`${this.adminBase}/classes/${classId}/assignments`, { params });
  }

  assignTeacher(req: AssignTeacherRequest): Observable<TeachingAssignmentDto> {
    return this.http.post<TeachingAssignmentDto>(`${this.adminBase}/teaching-assignments`, req);
  }

  unassignTeacher(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminBase}/teaching-assignments/${id}`);
  }

  // ── Enrollments ─────────────────────────────────────────────────────
  enrollStudent(req: EnrollStudentRequest): Observable<any> {
    return this.http.post<any>(`${this.adminBase}/enrollments`, req);
  }

  unenrollStudent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.adminBase}/enrollments/${id}`);
  }

  // ── Teachers ────────────────────────────────────────────────────────
  getTeachers(): Observable<TeacherDto[]> {
    return this.http.get<TeacherDto[]>(`${this.adminBase}/teachers`);
  }
}
