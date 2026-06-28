import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { StudentCourse } from '../../models';

@Component({
  selector: 'app-student-course-card',
  standalone: true,
  templateUrl: './course-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentCourseCardComponent {
  readonly course = input.required<StudentCourse>();
  readonly selected = input<boolean>(false);
  readonly viewDetails = output<StudentCourse>();
}
