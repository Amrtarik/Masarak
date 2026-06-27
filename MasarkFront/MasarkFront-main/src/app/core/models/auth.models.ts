// ─── Auth DTOs — mirrors AuthDTOs.cs exactly ────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phone?: string;
  country?: string;
  role: 'Admin' | 'Teacher' | 'Student' | 'Parent';
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface RevokeTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface AuthResponse {
  success: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiry: string | null;
  refreshTokenExpiry: string | null;
  user: UserInfoDto | null;
  error: string | null;
}

export interface UserInfoDto {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  phone: string | null;
  country: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface MessageResponse {
  success: boolean;
  message: string;
}

export interface LinkageCodeResponse {
  code: string;
}

// ─── Stored Auth State ───────────────────────────────────────────────────────

export interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  user: UserInfoDto;
}