import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { StudentScheduleSession } from '../../models';

@Component({
  selector: 'app-student-schedule-card',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './schedule-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentScheduleCardComponent {
  readonly session = input.required<StudentScheduleSession>();
  readonly viewDetails = output<StudentScheduleSession>();
  readonly joinSession = output<StudentScheduleSession>();

  formatDuration(duration: string | number): string {
    if (typeof duration === 'number') {
      return Number.isFinite(duration) ? `${duration} min` : 'Not specified';
    }

    return duration.trim().length > 0 ? duration : 'Not specified';
  }
}
