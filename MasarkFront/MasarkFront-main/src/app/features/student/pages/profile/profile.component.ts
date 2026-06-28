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

import { StudentClass, StudentGrade, StudentProfile } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-profile-page',
  standalone: true,
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentProfilePageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly profile = signal<StudentProfile | null>(null);
  readonly classInfo = signal<StudentClass | null>(null);
  readonly grades = signal<StudentGrade[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  readonly topGrade = computed(() => {
    const grades = this.grades();

    if (grades.length === 0) {
      return null;
    }

    return grades.reduce((best, grade) => (grade.finalGrade > best.finalGrade ? grade : best));
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService
      .getMyClass()
      .pipe(
        switchMap((classInfo) => {
          this.classInfo.set(classInfo);

          return forkJoin({
            profile: this.studentService.getProfile(classInfo?.academicYear),
            grades: this.studentService.getGrades(),
          });
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ profile, grades }) => {
          this.profile.set(profile);
          this.grades.set(grades);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.profile.set(null);
          this.classInfo.set(null);
          this.grades.set([]);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }
}
