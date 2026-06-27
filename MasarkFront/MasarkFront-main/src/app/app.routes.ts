import { Routes } from '@angular/router';
import { Chat } from './features/signalR/Component/chat/chat';
import { Landing } from './features/landing/landing';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout';
import { AdminComponent } from './features/admin/adminComponent/admin';
import { TeacherComponent } from './features/teacher/teacherComponent/teacher';
import { TeacherCourses } from './features/teacher/components/teacher-courses/teacher-courses';
import { UserManagement } from './features/admin/components/user-management/user-management';
import { TeacherStudents } from './features/teacher/components/teacher-students/teacher-students';
import { TeacherAssignments } from './features/teacher/components/teacher-assignments/teacher-assignments';
import { guestGuard } from './core/guards/guest-guard-guard';
import { AcademicManagementComponent } from './features/admin/components/academic-management/academic-management';
import { AdminPerformanceComponent } from './features/admin/components/admin-performance/admin-performance';
import { TeachersDirectoryComponent } from './features/academic/pages/admin/teachers-list/teachers-list.component';
// import { AuthGuard, AdminGuard, TeacherGuard, StudentGuard, SubscriptionGuard } from '../core/guards';

export const routes: Routes = [
  {
    path: '',
    component: Landing,
    pathMatch: 'full',
  },

  {
    path: 'auth',
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/signup/signup').then((m) => m.Signup),
      },
      {
        path: 'ChangePassword',
        loadComponent: () =>
          import('./features/auth/change-password-component/change-password-component').then(
            (m) => m.ChangePasswordComponent,
          ),
      },

      {
        path: 'ForgotPassword',
        loadComponent: () =>
          import('./features/auth/forgot-password-component/forgot-password-component').then(
            (m) => m.ForgotPasswordComponent,
          ),
      },

      {
        path: 'ResetPassword',
        loadComponent: () =>
          import('./features/auth/reset-password-component/reset-password-component').then(
            (m) => m.ResetPasswordComponent,
          ),
      },
    ],
  },

  {
    path: 'dashboard',
    component: DashboardLayoutComponent, // الشاسيه الثابت
    children: [
      {
        path: 'student',
        loadChildren: () =>
          import('./features/student/routes/student.routes').then((m) => m.STUDENT_ROUTES),
          // يفتح لوحة التحكم على رابط: dashboard/student
          
      }, // محتوى الطالب
      {
        path: 'teacher',
        children: [
          { path: '', component: TeacherComponent },
          { path: 'courses', component: TeacherCourses },
          { path: 'students', component: TeacherStudents },
          { path: 'assignments', component: TeacherAssignments },
        ],
      }, // محتوى المدرس
      {
        path: 'admin',
        children: [
          { path: '', component: AdminComponent },

          { path: 'users', component: UserManagement },
          { path: 'teachers', component: TeachersDirectoryComponent },
          { path: 'academic', component: AcademicManagementComponent },
          { path: 'performance', component: AdminPerformanceComponent },
        ],
      }, // محتوى المدير
    ],
  },
  {
    path: 'chat',
    component: Chat,
  },

  {
    path: 'plans',
    loadComponent: () =>
      import('./features/subscription/pages/plans/plans.component').then(
        (m) => m.PlansPageComponent,
      ),
  },

  {
    path: 'my-subscription',
    loadComponent: () =>
      import('./features/subscription/pages/my-subscription/my-subscription.component').then(
        (m) => m.MySubscriptionPageComponent,
      ),
  },

  {
    path: 'checkout-success',
    loadComponent: () =>
      import('./features/subscription/pages/checkout-success/checkout-success.component').then(
        (m) => m.CheckoutSuccessPageComponent,
      ),
  },

  {
    path: 'checkout-cancel',
    loadComponent: () =>
      import('./features/subscription/pages/checkout-cancel/checkout-cancel.component').then(
        (m) => m.CheckoutCancelPageComponent,
      ),
  },

    { path: '**', redirectTo: '' },

];
