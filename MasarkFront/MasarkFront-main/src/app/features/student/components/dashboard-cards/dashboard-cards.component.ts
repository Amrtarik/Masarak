import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import {
  StudentAttendanceSummary,
  StudentClass,
  StudentCourse,
  StudentPerformance,
  StudentProfile,
  StudentScheduleSession,
} from '../../models';

@Component({
  selector: 'app-student-dashboard-cards',
  standalone: true,
  templateUrl: './dashboard-cards.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentDashboardCardsComponent {
  readonly profile = input<StudentProfile | null>(null);
  readonly classInfo = input<StudentClass | null>(null);
  readonly courses = input<StudentCourse[]>([]);
  readonly attendance = input<StudentAttendanceSummary | null>(null);
  readonly performance = input<StudentPerformance[]>([]);
  readonly schedule = input<StudentScheduleSession[]>([]);

  readonly attendanceRate = computed(() => {
    const rate = this.attendance()?.attendanceRate ?? 0;
    return Math.min(Math.max(Math.round(rate), 0), 100);
  });

  readonly averagePerformance = computed(() => {
    const rows = this.performance();

    if (rows.length === 0) {
      return 0;
    }

    const total = rows.reduce((sum, row) => sum + row.finalGrade, 0);
    return Math.round(total / rows.length);
  });
}
