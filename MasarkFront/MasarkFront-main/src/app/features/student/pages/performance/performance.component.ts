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
import { forkJoin, switchMap } from 'rxjs';

import { StudentPerformanceTableComponent } from '../../components/performance-table/performance-table.component';
import { StudentGrade, StudentPerformance } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-performance-page',
  standalone: true,
  imports: [StudentPerformanceTableComponent],
  templateUrl: './performance.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentPerformancePageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly grades = signal<StudentGrade[]>([]);
  readonly performance = signal<StudentPerformance[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  readonly averageGrade = computed(() => {
    const grades = this.grades();

    if (grades.length === 0) {
      const performance = this.performance();

      if (performance.length === 0) {
        return 0;
      }

      const total = performance.reduce((sum, row) => sum + row.finalGrade, 0);
      return Math.round(total / performance.length);
    }

    const total = grades.reduce((sum, grade) => sum + grade.finalGrade, 0);
    return Math.round(total / grades.length);
  });

  ngOnInit(): void {
    this.loadPerformance();
  }

  loadPerformance(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService
      .getMyClass()
      .pipe(
        switchMap((classInfo) =>
          forkJoin({
            grades: this.studentService.getGrades(),
            performance: this.studentService.getPerformance(classInfo?.academicYear),
          }),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ grades, performance }) => {
          this.grades.set(grades);
          this.performance.set(performance);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.grades.set([]);
          this.performance.set([]);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }
}
