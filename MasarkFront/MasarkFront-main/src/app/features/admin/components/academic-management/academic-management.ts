import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicApiService } from '../../../../core/services/academic-api-service';
import { AdminApiService } from '../../../../core/services/admin-api-service';

@Component({
  selector: 'app-academic-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './academic-management.html',
  styleUrl: './academic-management.css',
})
export class AcademicManagementComponent implements OnInit {
  private readonly api = inject(AcademicApiService);
  private readonly adminApi = inject(AdminApiService);

  activeTab = signal<'grades' | 'subjects' | 'classes' | 'roster' | 'teachers'>('grades');
  
  // Data State
  grades = signal<any[]>([]);
  subjects = signal<any[]>([]);
  classes = signal<any[]>([]);
  roster = signal<any[]>([]);
  teachers = signal<any[]>([]);
  availableTeachers = signal<any[]>([]);
  availableStudents = signal<any[]>([]);

  // Selection State
  selectedGrade = signal<number | null>(null);
  selectedClass = signal<number | null>(null);
  academicYear = signal<string>('2026');

  // Loading & Modals
  isLoading = signal<boolean>(false);
  isModalOpen = signal<boolean>(false);
  modalMode = signal<'create' | 'edit'>('create');
  modalType = signal<'grade' | 'subject' | 'class'>('grade');

  // Form State
  formData = signal<any>({});

  ngOnInit(): void {
    this.loadGrades();
    this.loadAvailableTeachers();
    this.loadAvailableStudents();
  }

  loadAvailableTeachers() {
    this.adminApi.getUsers(1, 1000, 'Teacher').subscribe({
      next: (res: any) => {
        const list = Array.isArray(res) ? res : (res?.items || []);
        this.availableTeachers.set(list);
      }
    });
  }

  loadAvailableStudents() {
    this.adminApi.getUsers(1, 1000, 'Student').subscribe({
      next: (res: any) => {
        const list = Array.isArray(res) ? res : (res?.items || []);
        this.availableStudents.set(list);
      }
    });
  }

  setTab(tab: 'grades' | 'subjects' | 'classes' | 'roster' | 'teachers') {
    this.activeTab.set(tab);
    if (tab === 'grades') this.loadGrades();
    if (tab === 'subjects' && this.selectedGrade()) this.loadSubjects();
    if (tab === 'classes' && this.selectedGrade()) this.loadClasses();
    if (tab === 'roster' && this.selectedClass()) this.loadRoster();
    if (tab === 'teachers' && this.selectedClass()) {
      this.loadTeachers();
      // Subjects must be loaded for the assignment dropdown — use the class's grade
      if (!this.subjects().length && this.selectedGrade()) this.loadSubjects();
    }
  }

  // ── LOADERS ──
  loadGrades() {
    this.isLoading.set(true);
    this.api.getGrades().subscribe({
      next: (res) => { this.grades.set(res); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  loadSubjects() {
    const gid = this.selectedGrade();
    if (!gid) return;
    this.isLoading.set(true);
    this.api.getSubjectsByGrade(gid).subscribe({
      next: (res) => { this.subjects.set(res); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  loadClasses() {
    const gid = this.selectedGrade();
    if (!gid) return;
    this.isLoading.set(true);
    this.api.getClassesByGrade(gid, this.academicYear()).subscribe({
      next: (res) => { this.classes.set(res); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  loadRoster() {
    const cid = this.selectedClass();
    if (!cid) return;
    this.isLoading.set(true);
    this.api.getClassRoster(cid).subscribe({
      next: (res) => { this.roster.set(res); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  loadTeachers() {
    const cid = this.selectedClass();
    if (!cid) return;
    this.isLoading.set(true);
    this.api.getAssignmentsForClass(cid, this.academicYear()).subscribe({
      next: (res) => { this.teachers.set(res); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  // ── UI HELPERS ──
  onGradeSelect(gid: number) {
    this.selectedGrade.set(gid);
    this.selectedClass.set(null);
    if (this.activeTab() === 'subjects') this.loadSubjects();
    if (this.activeTab() === 'classes') this.loadClasses();
  }

  onClassSelect(cid: number) {
    this.selectedClass.set(cid);
    if (this.activeTab() === 'roster') this.loadRoster();
    if (this.activeTab() === 'teachers') {
      this.loadTeachers();
      // Always refresh subjects for the selected grade when on teachers tab
      if (this.selectedGrade()) this.loadSubjects();
    }
  }

  // ── MODALS & CRUD ──
  openModal(type: 'grade' | 'subject' | 'class', mode: 'create' | 'edit', data?: any) {
    this.modalType.set(type);
    this.modalMode.set(mode);
    this.formData.set(data ? { ...data } : {});
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.formData.set({});
  }

  saveModal() {
    const type = this.modalType();
    const mode = this.modalMode();
    const data = this.formData();

    if (type === 'grade') {
      const req = {
        name: data.name,
        nameAr: data.nameAr || undefined,
        stage: Number(data.stage ?? 0),
        order: Number(data.order ?? 1),
      };
      if (mode === 'create') {
        this.api.createGrade(req).subscribe({ next: () => { this.closeModal(); this.loadGrades(); }, error: () => alert('فشل إنشاء المرحلة. تأكد من ملء جميع الحقول.') });
      } else {
        this.api.updateGrade(data.gradeId, { name: data.name, nameAr: data.nameAr, isActive: data.isActive ?? true }).subscribe({ next: () => { this.closeModal(); this.loadGrades(); }, error: () => alert('فشل تحديث المرحلة.') });
      }
    }

    if (type === 'subject') {
      if (mode === 'create') {
        const req = {
          gradeId: this.selectedGrade()!,
          name: data.name,
          nameAr: data.nameAr || undefined,
          code: data.code,    // REQUIRED
        };
        this.api.createSubject(req).subscribe({ next: () => { this.closeModal(); this.loadSubjects(); }, error: () => alert('فشل إنشاء المادة. تأكد من ملء جميع الحقول (الاسم والرمز مطلوبان).') });
      } else {
        this.api.updateSubject(data.subjectId, { name: data.name, nameAr: data.nameAr, isActive: data.isActive ?? true }).subscribe({ next: () => { this.closeModal(); this.loadSubjects(); }, error: () => alert('فشل تحديث المادة.') });
      }
    }

    if (type === 'class') {
      if (mode === 'create') {
        const req = {
          gradeId: this.selectedGrade()!,
          name: data.name,
          maxCapacity: Number(data.maxCapacity ?? 30),  // REQUIRED
          academicYear: Number(this.academicYear()),     // REQUIRED int
        };
        this.api.createClass(req).subscribe({ next: () => { this.closeModal(); this.loadClasses(); }, error: () => alert('فشل إنشاء الفصل. تأكد من ملء جميع الحقول.') });
      } else {
        this.api.updateClass(data.classId, { name: data.name, maxCapacity: Number(data.maxCapacity ?? 30), isActive: data.isActive ?? true }).subscribe({ next: () => { this.closeModal(); this.loadClasses(); }, error: () => alert('فشل تحديث الفصل.') });
      }
    }
  }

  // ── ROSTER / TEACHERS ACTIONS ──
  enrollStudent(studentIdStr: string) {
    const cid = this.selectedClass();
    if (!cid || !studentIdStr) return;
    this.api.enrollStudent({
      studentId: +studentIdStr,
      classId: cid,
      academicYear: Number(this.academicYear()),  // REQUIRED int
    }).subscribe({
      next: () => this.loadRoster(),
      error: (err) => {
        const msg = err.error?.message || 'فشل تسجيل الطالب. تأكد من صحة رقم الطالب ورقم الفصل.';
        alert('خطأ: ' + msg);
      }
    });
  }

  unenrollStudent(studentClassId: number) {
    if(confirm('هل أنت متأكد من إلغاء قيد الطالب؟')) {
      this.api.unenrollStudent(studentClassId).subscribe({
        next: () => this.loadRoster(),
        error: () => alert('فشل إلغاء القيد.')
      });
    }
  }

  assignTeacher(teacherIdStr: string, subjectIdStr: string) {
    const cid = this.selectedClass();
    const tId = parseInt(teacherIdStr, 10);
    const sId = parseInt(subjectIdStr, 10);

    if (!cid) {
      alert('الرجاء اختيار الفصل أولاً');
      return;
    }
    if (isNaN(tId) || isNaN(sId)) {
      alert('تأكد من إدخال أرقام صحيحة لرقم المعلم ورقم المادة.');
      return;
    }

    this.api.assignTeacher({
      classId: cid,
      subjectId: sId,
      teacherId: tId,
      academicYear: Number(this.academicYear()),  // REQUIRED int
    }).subscribe({
      next: () => this.loadTeachers(),
      error: (err) => {
        const msg = err.error?.message || 'فشل تعيين المعلم. تأكد من صحة رقم المعلم ورقم المادة.';
        alert('خطأ: ' + msg);
      }
    });
  }

  unassignTeacher(assignmentId: number) {
    if(confirm('هل أنت متأكد من إلغاء تعيين المعلم؟')) {
      this.api.unassignTeacher(assignmentId).subscribe({
        next: () => this.loadTeachers(),
        error: () => alert('فشل إلغاء تعيين المعلم.')
      });
    }
  }
}
