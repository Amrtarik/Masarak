import { DecimalPipe } from '@angular/common';
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
import { switchMap } from 'rxjs';

import { StudentAttendanceSummaryComponent } from '../../components/attendance-summary/attendance-summary.component';
import { StudentAttendanceSummary } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-attendance-page',
  standalone: true,
  imports: [DecimalPipe, StudentAttendanceSummaryComponent],
  templateUrl: './attendance.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentAttendancePageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly summary = signal<StudentAttendanceSummary | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  readonly subjects = computed(() => this.summary()?.subjects ?? []);

  ngOnInit(): void {
    this.loadAttendance();
  }

  loadAttendance(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService
      .getMyClass()
      .pipe(
        switchMap((classInfo) => this.studentService.getAttendance(classInfo?.academicYear)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (summary) => {
          this.summary.set(summary);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.summary.set(null);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  percentageBadgeClass(percentage: number): string {
    const baseClass = 'inline-flex rounded-full px-3 py-1 text-xs font-bold ring-1';

    if (percentage >= 85) {
      return `${baseClass} bg-emerald-50 text-emerald-700 ring-emerald-200`;
    }

    if (percentage >= 60) {
      return `${baseClass} bg-amber-50 text-amber-700 ring-amber-200`;
    }

    return `${baseClass} bg-rose-50 text-rose-700 ring-rose-200`;
  }
}
