import { DatePipe } from '@angular/common';
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

import { StudentContentGroup, StudentContentItem } from '../../models';
import { StudentService } from '../../services/student.service';

@Component({
  selector: 'app-student-content-page',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './content.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentContentPageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly groups = signal<StudentContentGroup[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  readonly totalItems = computed(() =>
    this.groups().reduce((total, group) => total + group.items.length, 0),
  );

  ngOnInit(): void {
    this.loadContent();
  }

  loadContent(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.studentService
      .getContentLibrary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (groups) => {
          this.groups.set(groups);
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.groups.set([]);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  contentUrl(item: StudentContentItem): string | null {
    return item.url ?? item.fileUrl ?? null;
  }

  contentTypeLabel(item: StudentContentItem): string {
    return item.contentType || 'Resource';
  }

  countByType(group: StudentContentGroup, type: 'pdf' | 'video' | 'resource'): number {
    return group.items.filter((item) => {
      const contentType = (item.contentType ?? '').toLowerCase();

      if (type === 'pdf') {
        return contentType.includes('pdf');
      }

      if (type === 'video') {
        return contentType.includes('video');
      }

      return !contentType.includes('pdf') && !contentType.includes('video');
    }).length;
  }
}
