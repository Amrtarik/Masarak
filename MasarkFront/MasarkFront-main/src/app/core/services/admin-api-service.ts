import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminUserDto {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface AdminUsersPagedResult {
  items: AdminUserDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminCreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
  phone?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/admin`;

  /** GET /api/admin/users?pageNumber=&pageSize=&role= */
  getUsers(
    pageNumber = 1,
    pageSize = 100,
    role?: string,
  ): Observable<AdminUsersPagedResult | AdminUserDto[]> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (role) params = params.set('role', role);
    return this.http.get<AdminUsersPagedResult | AdminUserDto[]>(`${this.base}/users`, { params });
  }

  /** POST /api/admin/users — create a real user with any role */
  createUser(req: AdminCreateUserRequest): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${this.base}/users`, req);
  }

  /** DELETE /api/admin/users/{id} */
  deleteUser(userId: number): Observable<any> {
    return this.http.delete<any>(`${this.base}/users/${userId}`);
  }

  /** GET /api/admin/dashboard */
  getDashboard(): Observable<any> {
    return this.http.get<any>(`${this.base}/dashboard`);
  }

  /** GET /api/admin/performance/classes/{classId}/subjects/{subjectId} */
  getClassReport(classId: number, subjectId: number, academicYear: string): Observable<any> {
    let params = new HttpParams().set('academicYear', academicYear);
    return this.http.get<any>(`${this.base}/performance/classes/${classId}/subjects/${subjectId}`, { params });
  }
}
