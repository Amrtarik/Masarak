import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthApiService } from '../services/auth-api-service';
import { AuthStateService } from '../services/auth-state-service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authApi = inject(AuthApiService);
  const authState = inject(AuthStateService);
  const token = localStorage.getItem('masarak_access_token');

  if (!token) {
    return next(req);
  }

  const cloned = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status !== 401 ||
        req.url.includes('/auth/refresh') ||
        req.url.includes('/auth/login')
      ) {
        return throwError(() => error);
      }

      const refreshToken = authState.getRefreshToken();
      if (!refreshToken) {
        authState.clearAuth();
        return throwError(() => error);
      }

      return authApi.refresh(token, refreshToken).pipe(
        switchMap((res) => {
          if (!res.success || !res.accessToken || !res.refreshToken) {
            authState.clearAuth();
            return throwError(() => error);
          }

          authState.setAuth({
            accessToken: res.accessToken,
            refreshToken: res.refreshToken,
            user: res.user!,
          });

          const retried = req.clone({
            setHeaders: {
              Authorization: `Bearer ${res.accessToken}`,
            },
          });

          return next(retried);
        }),
        catchError(() => {
          authState.clearAuth();
          return throwError(() => error);
        }),
      );
    }),
  );
};
