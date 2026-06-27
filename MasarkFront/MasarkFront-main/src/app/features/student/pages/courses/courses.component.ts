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

import { StudentCourseCardComponent } from '../../components/course-card/course-card.component';
import { StudentCourse, StudentEntityId } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-courses-page',
  standalone: true,
  imports: [RouterLink, StudentCourseCardComponent],
  templateUrl: './courses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentCoursesPageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly courses = signal<StudentCourse[]>([]);
  readonly selectedSubjectId = signal<StudentEntityId | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  readonly selectedCourse = computed(() => {
    const courses = this.courses();
    const selectedSubjectId = this.selectedSubjectId();

    return (
      courses.find((course) => String(course.subjectId) === String(selectedSubjectId)) ??
      courses[0] ??
      null
    );
  });

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService
      .getCourses()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (courses) => {

          console.log(courses)
          this.courses.set(courses);
          this.selectedSubjectId.set(courses[0]?.subjectId ?? null);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.courses.set([]);
          this.selectedSubjectId.set(null);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  selectCourse(course: StudentCourse): void {
    this.selectedSubjectId.set(course.subjectId);
  }
}
