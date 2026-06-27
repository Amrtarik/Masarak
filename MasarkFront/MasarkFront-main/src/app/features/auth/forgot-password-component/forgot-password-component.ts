import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api-service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password-component.html'
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);

  // استخدام الـ Signals لإدارة الحالة بسلاسة
  loading = signal(false);
  error = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  submitted = signal(false);

  // تعيين الحقول كـ nonNullable لضمان قوة أنواع البيانات Typed Forms
  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  // توليد كلاسات الحقول ديناميكياً بـ Tailwind CSS بناءً على حالة الـ Validation
  fieldClass(name: string): string {
    const ctrl = this.form.get(name);
    const isInvalid = !!(ctrl?.invalid && (ctrl?.touched || this.submitted()));
    
    const baseClass = "w-full px-4 py-3 bg-slate-50 border rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:transition duration-200";
    return isInvalid
      ? `${baseClass} border-red-400 focus:ring-red-200 focus:bg-white`
      : `${baseClass} border-slate-200 focus:ring-blue-600 focus:border-transparent focus:bg-white`;
  }

  showError(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl?.invalid && (ctrl?.touched || this.submitted()));
  }

  emailError(): string {
    const ctrl = this.form.get('email');
    if (ctrl?.hasError('required')) return 'البريد الإلكتروني مطلوب';
    if (ctrl?.hasError('email')) return 'صيغة البريد الإلكتروني غير صحيحة';
    return '';
  }

  submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    
    if (this.form.invalid) return;

    this.loading.set(true);
    const { email } = this.form.getRawValue();

    this.authApi.forgotPassword({ email }).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.success) {
          this.successMessage.set(res.message || 'تم إرسال رمز إعادة التعيين بنجاح.');
        } else {
          this.error.set(res.message ?? 'فشل طلب إعادة تعيين كلمة المرور.');
        }
      },
      error: err => {
        this.loading.set(false);
        this.error.set(
          err?.error?.message ?? err?.error?.error ?? 'حدث خطأ غير متوقع أثناء الاتصال بالسيرفر.'
        );
      },
    });
  }
}