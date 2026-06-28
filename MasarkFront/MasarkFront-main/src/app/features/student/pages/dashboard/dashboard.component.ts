import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { StudentAttendanceSummaryComponent } from '../../components/attendance-summary/attendance-summary.component';
import { StudentCourseCardComponent } from '../../components/course-card/course-card.component';
import { StudentDashboardCardsComponent } from '../../components/dashboard-cards/dashboard-cards.component';
import { StudentScheduleCardComponent } from '../../components/schedule-card/schedule-card.component';
import { StudentDashboardData, StudentScheduleSession } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
    StudentAttendanceSummaryComponent,
    StudentCourseCardComponent,
    StudentDashboardCardsComponent,
    StudentScheduleCardComponent,
  ],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentDashboardPageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly data = signal<StudentDashboardData | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);

  readonly recentCourses = computed(() => this.data()?.courses.slice(0, 4) ?? []);
  readonly upcomingSchedule = computed(() => this.data()?.schedule.slice(0, 3) ?? []);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.actionMessage.set(null);

    this.studentService
      .getDashboardData(2026)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.data.set(data);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.data.set(null);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  joinSession(session: StudentScheduleSession): void {
    if (session.sessionId === undefined) {
      return;
    }

    this.studentService
      .joinSession(session.sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.actionMessage.set('Session join recorded.'),
        error: (error: unknown) =>
          this.actionMessage.set(this.studentService.resolveErrorMessage(error)),
      });
  }
}
