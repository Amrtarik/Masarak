import { Routes } from '@angular/router';

export const STUDENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../pages/dashboard/dashboard.component').then((m) => m.StudentDashboardPageComponent),
  },
  {
    path: 'dashboard',
    redirectTo: '',
    pathMatch: 'full',
  },
  {
    path: 'courses',
    loadComponent: () =>
      import('../pages/courses/courses.component').then((m) => m.StudentCoursesPageComponent),
  },
  {
    path: 'content',
    loadComponent: () =>
      import('../pages/content/content.component').then((m) => m.StudentContentPageComponent),
  },
  {
    path: 'library',
    redirectTo: 'content',
    pathMatch: 'full',
  },
  {
    path: 'assignments',
    loadComponent: () =>
      import('../pages/assignments/assignments.component').then(
        (m) => m.StudentAssignmentsPageComponent,
      ),
  },
  {
    path: 'exams',
    loadComponent: () =>
      import('../pages/exams/exams.component').then((m) => m.StudentExamsPageComponent),
  },
  {
    path: 'attendance',
    loadComponent: () =>
      import('../pages/attendance/attendance.component').then(
        (m) => m.StudentAttendancePageComponent,
      ),
  },
  {
    path: 'performance',
    loadComponent: () =>
      import('../pages/performance/performance.component').then(
        (m) => m.StudentPerformancePageComponent,
      ),
  },
  {
    path: 'schedule',
    loadComponent: () =>
      import('../pages/schedule/schedule.component').then((m) => m.StudentSchedulePageComponent),
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('../pages/profile/profile.component').then((m) => m.StudentProfilePageComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
