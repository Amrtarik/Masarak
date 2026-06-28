import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { CommonModule } from '@angular/common';
import { AuthApiService } from '../../../core/services/auth-api-service';
import { AuthStateService } from '../../../core/services/auth-state-service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
})
export class Login {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  public authState = inject(AuthStateService); // عملناها public عشان نستخدم الـ Signals في الـ HTML
  private router = inject(Router);

  // بناء الفورم مع الـ Validation
  loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.authState.setLoading(true);
    this.authState.setError(null);

    const credentials = this.loginForm.getRawValue();

    this.authApi.login(credentials).subscribe({
      next: (res) => {
        this.authState.setLoading(false);

        if (res.success && res.accessToken && res.refreshToken && res.user) {
          // حفظ البيانات في الـ State والـ LocalStorage
          this.authState.setAuth({
            accessToken: res.accessToken,
            refreshToken: res.refreshToken,
            user: res.user,
          });

          // التوجيه التلقائي حسب دور المستخدم
          this.redirectBasedOnRole(res.user.role);
        } else {
          this.authState.setError(res.error || 'بيانات الدخول غير صحيحة');
        }
      },
      error: (err) => {
        this.authState.setLoading(false);
        this.authState.setError('حدث خطأ في الاتصال بالسيرفر، حاول مجدداً');
      },
    });
  }

  private redirectBasedOnRole(role: string) {
    if (role === 'Admin') this.router.navigate(['/dashboard/admin']);
    else if (role === 'Teacher') this.router.navigate(['/dashboard/teacher']);
    else if (role === 'Student') this.router.navigate(['/dashboard/student']);
    else if (role === 'Parent') this.router.navigate(['/chat']);
    else this.router.navigate(['/chat']);
  }
}
