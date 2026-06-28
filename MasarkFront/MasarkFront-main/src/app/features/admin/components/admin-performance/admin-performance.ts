import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../../../core/services/admin-api-service';
import { AcademicApiService } from '../../../../core/services/academic-api-service';
import { ClassPerformanceReportDto } from '../../../../models/academic.model';

@Component({
  selector: 'app-admin-performance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-performance.html',
  styleUrl: './admin-performance.css',
})
export class AdminPerformanceComponent implements OnInit {
  private readonly adminApi = inject(AdminApiService);
  private readonly academicApi = inject(AcademicApiService);

  grades = signal<any[]>([]);
  subjects = signal<any[]>([]);
  classes = signal<any[]>([]);

  selectedGrade = signal<number | null>(null);
  selectedSubject = signal<number | null>(null);
  selectedClass = signal<number | null>(null);
  academicYear = signal<string>('2026');

  report = signal<ClassPerformanceReportDto | null>(null);
  isLoading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.academicApi.getGrades().subscribe((grades) => {
      this.grades.set(grades);
    });
  }

  onGradeChange(): void {
    const gid = this.selectedGrade();
    this.selectedSubject.set(null);
    this.selectedClass.set(null);
    this.report.set(null);
    this.subjects.set([]);
    this.classes.set([]);

    if (gid) {
      this.academicApi.getSubjectsByGrade(gid).subscribe((res) => this.subjects.set(res));
      this.academicApi.getClassesByGrade(gid, this.academicYear()).subscribe((res) => this.classes.set(res));
    }
  }

  loadReport(): void {
    const cid = this.selectedClass();
    const sid = this.selectedSubject();
    if (!cid || !sid) return;

    this.isLoading.set(true);
    this.error.set(null);
    this.report.set(null);

    this.adminApi.getClassReport(cid, sid, this.academicYear()).subscribe({
      next: (res) => {
        this.report.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('لم يتم العثور على تقرير لهذه المادة في هذا الفصل، أو حدث خطأ.');
        this.isLoading.set(false);
      },
    });
  }
}
