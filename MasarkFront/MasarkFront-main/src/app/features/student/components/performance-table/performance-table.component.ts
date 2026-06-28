import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { StudentPerformance } from '../../models';

@Component({
  selector: 'app-student-performance-table',
  standalone: true,
  templateUrl: './performance-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentPerformanceTableComponent {
  readonly rows = input<StudentPerformance[]>([]);

  formatScore(score: number): string {
    return Number.isFinite(score) ? score.toFixed(1) : '0.0';
  }
}
