import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  RevokeTokenRequest,
  ChangePasswordRequest,
  AuthResponse,
  MessageResponse,
  LinkageCodeResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, req);
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/register`, req);
  }

  /** POST /api/auth/logout  body: { refreshToken } */
  logout(refreshToken: string): Observable<MessageResponse> {
    const body: RevokeTokenRequest = { refreshToken };
    return this.http.post<MessageResponse>(`${this.base}/logout`, body);
  }

  /** POST /api/auth/refresh  body: { accessToken, refreshToken } */
  refresh(accessToken: string, refreshToken: string): Observable<AuthResponse> {
    const body: RefreshTokenRequest = { accessToken, refreshToken };
    return this.http.post<AuthResponse>(`${this.base}/refresh`, body);
  }

  changePassword(req: ChangePasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/change-password`, req);
  }

  forgotPassword(req: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/forgot-password`, req);
  }

  resetPassword(req: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/reset-password`, req);
  }

  /** GET /api/auth/my-linkage-code  — Student role only */
  getMyLinkageCode(): Observable<LinkageCodeResponse> {
    return this.http.get<LinkageCodeResponse>(`${this.base}/my-linkage-code`);
  }
}
