import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/services/auth-api-service';
import { AuthStateService } from '../../../core/services/auth-state-service';
import { RegisterRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './Signup.html',
})
export class Signup {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  public authState = inject(AuthStateService);
  private router = inject(Router);



  // بناء حقول الفورم مع الـ Validation
  registerForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
    phone: [''],
    country: [''],
    role: ['Student' as 'Student' | 'Parent', [Validators.required]],
  });

  onSubmit() {
    // 1. التحقق من صحة الفورم
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const formValues = this.registerForm.getRawValue();

    // 2. التحقق من تطابق كلمتي المرور
    if (formValues.password !== formValues.confirmPassword) {
      this.authState.setError('كلمات المرور غير متطابقة');
      return;
    }

    // 3. بدء عملية التسجيل
    this.authState.setLoading(true);
    this.authState.setError(null);

    const requestData: RegisterRequest = {
      fullName: formValues.fullName,
      email: formValues.email,
      password: formValues.password,
      confirmPassword: formValues.confirmPassword,
      phone: formValues.phone || undefined,
      country: formValues.country || undefined,
      role: formValues.role,
    };

    this.authApi.register(requestData).subscribe({
      next: (res) => {
        this.authState.setLoading(false);
        if (res.success && res.accessToken && res.refreshToken && res.user) {
          // حفظ البيانات في الـ State والـ LocalStorage
          this.authState.setAuth({
            accessToken: res.accessToken,
            refreshToken: res.refreshToken,
            user: res.user,
          });

          // التوجيه للـ Dashboard الأساسية بعد التسجيل بنجاح
          this.router.navigate(['/auth/login']);
        } else {
          this.authState.setError(res.error || 'فشل إنشاء الحساب، يرجى المحاولة لاحقاً');
        }
      },
      error: () => {
        this.authState.setLoading(false);
        this.authState.setError('حدث خطأ غير متوقع أثناء الاتصال بالسيرفر');
      },
    });
  }
}
