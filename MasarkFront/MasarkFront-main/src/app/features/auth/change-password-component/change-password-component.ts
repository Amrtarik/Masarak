import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
  ReactiveFormsModule,
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api-service';
import { AuthStateService } from '../../../core/services/auth-state-service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './change-password-component.html',
})
export class ChangePasswordComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private authState = inject(AuthStateService);
  private router = inject(Router);

  // استخدام الـ Signals لإدارة حالة الواجهة
  loading = signal(false);
  error = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  submitted = signal(false);
  countdown = signal(3);

  // جعل الحقول nonNullable بـ Angular Strongly Typed Forms لعدم الحاجة للـ "!" لاحقاً
  form = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required],
    },
    { validators: this.passwordMatchValidator },
  );

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPass = control.get('newPassword')?.value;
    const confirmPass = control.get('confirmNewPassword')?.value;
    return newPass && confirmPass && newPass !== confirmPass ? { mismatch: true } : null;
  }

  // ميثود ديناميكية تخدم الـ Tailwind CSS كلاسس مباشرة من الـ Component
  fieldClass(name: string): string {
    const ctrl = this.form.get(name);
    const hasError =
      (ctrl?.invalid && (ctrl?.touched || this.submitted())) ||
      (name === 'confirmNewPassword' &&
        this.form.hasError('mismatch') &&
        (ctrl?.touched || this.submitted()));

    const baseClass =
      'w-full px-4 py-2.5 bg-slate-50 border rounded-xl text-slate-900 focus:outline-none focus:ring-2 focus:transition duration-200';
    return hasError
      ? `${baseClass} border-red-400 focus:ring-red-200 focus:bg-white`
      : `${baseClass} border-slate-200 focus:ring-blue-600 focus:border-transparent focus:bg-white`;
  }

  showError(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl?.invalid && (ctrl?.touched || this.submitted()));
  }

  newPasswordError(): string {
    const ctrl = this.form.get('newPassword');
    if (ctrl?.hasError('required')) return 'كلمة المرور الجديدة مطلوبة';
    if (ctrl?.hasError('minlength')) return 'يجب ألا تقل كلمة المرور عن 8 أحرف';
    return '';
  }

  confirmPasswordError(): string {
    const ctrl = this.form.get('confirmNewPassword');
    if (ctrl?.hasError('required')) return 'يرجى تأكيد كلمة المرور الجديدة';
    if (this.form.hasError('mismatch')) return 'كلمات المرور غير متطابقة';
    return '';
  }

  cancel(): void {
    this.router.navigate(['/dashboard']);
  }

  submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);

    // سحب القيم مباشرة بدون الحاجة لـ "!" لأن الفورم أصبح nonNullable
    const { currentPassword, newPassword, confirmNewPassword } = this.form.getRawValue();

    this.authApi
      .changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.successMessage.set(res.message || 'تم تغيير كلمة المرور بنجاح!');

            // عد تنازلي للتوجيه الذكي
            const interval = setInterval(() => {
              this.countdown.update((c) => c - 1);
              if (this.countdown() <= 0) {
                clearInterval(interval);
                this.authState.clearAuth();
                this.router.navigate(['/auth/login']); // ربطناها بمسار الـ auth الصح بتاعك
              }
            }, 1000);
          } else {
            this.error.set(res.message ?? 'فشل تغيير كلمة المرور.');
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(
            err?.error?.message ?? err?.error?.error ?? 'حدث خطأ غير متوقع أثناء الاتصال.',
          );
        },
      });
  }
}
