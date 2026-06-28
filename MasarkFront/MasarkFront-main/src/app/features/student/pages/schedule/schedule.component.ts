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

import { StudentScheduleCardComponent } from '../../components/schedule-card/schedule-card.component';
import { StudentEntityId, StudentScheduleSession } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-schedule-page',
  standalone: true,
  imports: [StudentScheduleCardComponent],
  templateUrl: './schedule.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentSchedulePageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly sessions = signal<StudentScheduleSession[]>([]);
  readonly selectedSessionId = signal<StudentEntityId | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly isJoining = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);

  readonly selectedSession = computed(() => {
    const selectedSessionId = this.selectedSessionId();
    const sessions = this.sessions();

    return (
      sessions.find((session) => String(session.sessionId) === String(selectedSessionId)) ??
      sessions[0] ??
      null
    );
  });

  ngOnInit(): void {
    this.loadSchedule();
  }

  loadSchedule(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.actionMessage.set(null);

    this.studentService
      .getMyClass()
      .pipe(
        switchMap((classInfo) => this.studentService.getSchedule(classInfo?.academicYear)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (sessions) => {
          this.sessions.set(sessions);
          this.selectedSessionId.set(sessions[0]?.sessionId ?? null);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.sessions.set([]);
          this.selectedSessionId.set(null);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  selectSession(session: StudentScheduleSession): void {
    this.selectedSessionId.set(session.sessionId ?? null);
  }

  joinSession(session: StudentScheduleSession): void {
    if (session.sessionId === undefined) {
      return;
    }

    this.isJoining.set(true);
    this.actionMessage.set(null);

    this.studentService
      .joinSession(session.sessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isJoining.set(false);
          this.actionMessage.set('Session join recorded.');
        },
        error: (error: unknown) => {
          this.isJoining.set(false);
          this.actionMessage.set(this.studentService.resolveErrorMessage(error));
        },
      });
  }
}
