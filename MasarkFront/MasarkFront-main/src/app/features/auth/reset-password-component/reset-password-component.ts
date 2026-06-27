import { Component, OnInit, inject, signal } from '@angular/core';
import {
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
  ReactiveFormsModule,
} from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api-service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password-component.html',
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private route = inject(ActivatedRoute);

  // استخدام الـ Signals الرشيقة لإدارة حالة الصفحة
  loading = signal(false);
  error = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  showNewPassword = signal(false);
  submitted = signal(false);

  // بناء الفورم كـ nonNullable لحماية الـ Types ومنع استخدام الـ ! اليدوية
  form = this.fb.nonNullable.group(
    {
      token: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required],
    },
    { validators: this.passwordMatchValidator },
  );

  ngOnInit(): void {
    // التقاط الـ Token تلقائياً من الـ URL إن وجد (Query Parameter)
    const tokenParam = this.route.snapshot.queryParamMap.get('token');
    if (tokenParam) {
      this.form.patchValue({ token: tokenParam });
    }
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPass = control.get('newPassword')?.value;
    const confirmPass = control.get('confirmNewPassword')?.value;
    return newPass && confirmPass && newPass !== confirmPass ? { mismatch: true } : null;
  }

  // توليد كلاسات الحقول ديناميكياً بـ Tailwind CSS بناءً على حالة الأخطاء
  fieldClass(name: string): string {
    const ctrl = this.form.get(name);
    const hasError =
      (ctrl?.invalid && (ctrl?.touched || this.submitted())) ||
      (name === 'confirmNewPassword' &&
        this.form.hasError('mismatch') &&
        (ctrl?.touched || this.submitted()));

    const baseClass =
      'w-full px-4 py-3 bg-slate-50 border rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:transition duration-200';
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

  submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    if (this.form.invalid) return;

    this.loading.set(true);

    // جلب القيم بأمان كامل وبدون استخدام ! لأننا اعتمدنا الـ nonNullable
    const { token, newPassword, confirmNewPassword } = this.form.getRawValue();

    this.authApi
      .resetPassword({
        token,
        newPassword,
        confirmNewPassword,
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.successMessage.set(res.message || 'تم إعادة تعيين كلمة المرور بنجاح!');
          } else {
            this.error.set(res.message ?? 'فشل إعادة تعيين كلمة المرور.');
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(
            err?.error?.message ?? err?.error?.error ?? 'حدث خطأ غير متوقع أثناء الاتصال بالسيرفر.',
          );
        },
      });
  }
}
