import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicApiService } from '../../../../../core/services/academic-api-service';
import { TeacherDto } from '../../../../../models/academic.model';

@Component({
  selector: 'app-teachers-directory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 max-w-7xl mx-auto space-y-6" dir="rtl">
      <!-- Header -->
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div>
          <span class="text-xs font-bold text-blue-600 bg-blue-50 px-3 py-1 rounded-full">إدارة النظام</span>
          <h1 class="text-2xl font-black text-slate-800 mt-2">دليل المعلمين</h1>
          <p class="text-slate-500 text-sm mt-1">عرض جميع المعلمين المسجلين في النظام، تخصصاتهم، والمواد التي يدرسونها.</p>
        </div>
        <div class="bg-blue-50 text-blue-700 px-4 py-3 rounded-xl border border-blue-100 flex items-center gap-3">
          <span class="text-2xl">👨‍🏫</span>
          <div>
            <div class="text-xs text-blue-600 font-medium">إجمالي المعلمين</div>
            <div class="text-xl font-bold">{{ teachers().length }} معلم</div>
          </div>
        </div>
      </div>

      <!-- Filters & Search -->
      <div class="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col md:flex-row gap-4">
        <div class="flex-1 relative">
          <input
            type="text"
            [(ngModel)]="searchQuery"
            placeholder="البحث باسم المعلم، البريد الإلكتروني، أو التخصص..."
            class="w-full pr-10 pl-4 py-2 border border-slate-200 rounded-lg text-sm outline-none focus:border-blue-500 bg-slate-50 focus:bg-white transition-all"
          />
          <span class="absolute right-3 top-2.5 text-slate-400">🔍</span>
        </div>
        <div class="w-full md:w-48">
          <select [(ngModel)]="selectedSpecialization" class="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm outline-none bg-slate-50 focus:bg-white transition-all">
            <option value="">كل التخصصات</option>
            @for (spec of specializations(); track spec) {
              <option [value]="spec">{{ spec }}</option>
            }
          </select>
        </div>
      </div>

      <!-- Loader -->
      @if (isLoading()) {
        <div class="flex justify-center py-16">
          <div class="w-10 h-10 border-4 border-blue-100 border-t-blue-600 rounded-full animate-spin"></div>
        </div>
      } @else {
        <!-- Teachers Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          @for (t of filteredTeachers(); track t.teacherId) {
            <div class="bg-white border border-slate-200 rounded-2xl shadow-sm hover:shadow-md hover:border-blue-300 transition-all flex flex-col overflow-hidden group">
              <!-- Card Top Header -->
              <div class="p-6 border-b border-slate-100 bg-gradient-to-br from-slate-50 to-slate-100/50 flex items-start gap-4">
                <div class="w-14 h-14 bg-blue-100 text-blue-700 rounded-2xl flex items-center justify-center font-black text-xl shadow-inner group-hover:scale-105 transition-transform">
                  {{ t.fullName.charAt(0) || 'T' }}
                </div>
                <div class="space-y-1 min-w-0 flex-1">
                  <h3 class="font-bold text-slate-800 text-lg truncate group-hover:text-blue-600 transition-colors">{{ t.fullName }}</h3>
                  @if (t.specialization) {
                    <span class="inline-block text-xs bg-indigo-50 text-indigo-700 font-semibold px-2.5 py-0.5 rounded-full border border-indigo-100">
                      {{ t.specialization }}
                    </span>
                  } @else {
                    <span class="inline-block text-xs bg-slate-100 text-slate-500 px-2.5 py-0.5 rounded-full">
                      بدون تخصص
                    </span>
                  }
                </div>
              </div>

              <!-- Card Body Info -->
              <div class="p-6 space-y-4 flex-1">
                <!-- Personal Details -->
                <div class="space-y-2 text-sm">
                  <div class="flex items-center gap-2 text-slate-600">
                    <span class="text-base">📧</span>
                    <span class="truncate">{{ t.email }}</span>
                  </div>
                  <div class="flex items-center gap-2 text-slate-500 text-xs">
                    <span class="text-base font-normal">📅</span>
                    <span>تاريخ التعيين: {{ t.hiringDate | date:'mediumDate' }}</span>
                  </div>
                </div>

                <!-- Bio -->
                @if (t.bio) {
                  <p class="text-xs text-slate-500 bg-slate-50 p-3 rounded-lg border border-slate-100 line-clamp-3 leading-relaxed">
                    {{ t.bio }}
                  </p>
                }

                <!-- Courses taught -->
                <div class="space-y-2 pt-2 border-t border-slate-100">
                  <h4 class="text-xs font-bold text-slate-400 uppercase tracking-wider">المواد والفصول المسندة:</h4>
                  @if (t.courses && t.courses.length > 0) {
                    <div class="flex flex-wrap gap-1.5 max-h-24 overflow-y-auto pr-1">
                      @for (c of t.courses; track c.assignmentId) {
                        <div class="text-xs bg-emerald-50 text-emerald-700 border border-emerald-100 px-2 py-1 rounded-md font-medium">
                          {{ c.subjectName }} - {{ c.className }}
                        </div>
                      }
                    </div>
                  } @else {
                    <p class="text-xs text-slate-400 italic">لا توجد مواد مسندة حالياً.</p>
                  }
                </div>
              </div>

              <!-- Card Footer Action -->
              <div class="px-6 py-4 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-xs">
                <span class="text-slate-400 font-medium">ID المستخدم: {{ t.userId }}</span>
                <span class="text-slate-400 font-medium">ID المعلم: {{ t.teacherId }}</span>
              </div>
            </div>
          } @empty {
            <div class="col-span-full py-16 text-center bg-white border border-slate-200 rounded-2xl shadow-sm">
              <span class="text-4xl block mb-3">🔍</span>
              <p class="text-slate-500 text-sm font-medium">لم يتم العثور على معلمين يطابقون خيارات البحث.</p>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .line-clamp-3 {
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
  `]
})
export class TeachersDirectoryComponent implements OnInit {
  private readonly api = inject(AcademicApiService);

  teachers = signal<TeacherDto[]>([]);
  isLoading = signal<boolean>(true);
  
  searchQuery = '';
  selectedSpecialization = '';

  // Get distinct specializations for dropdown filter
  specializations = computed(() => {
    const specs = this.teachers()
      .map(t => t.specialization)
      .filter((s): s is string => !!s);
    return Array.from(new Set(specs));
  });

  filteredTeachers = computed(() => {
    let list = this.teachers();
    const query = this.searchQuery.trim().toLowerCase();
    const spec = this.selectedSpecialization;

    if (query) {
      list = list.filter(t => 
        t.fullName?.toLowerCase().includes(query) ||
        t.email?.toLowerCase().includes(query) ||
        t.specialization?.toLowerCase().includes(query)
      );
    }

    if (spec) {
      list = list.filter(t => t.specialization === spec);
    }

    return list;
  });

  ngOnInit(): void {
    this.loadTeachers();
  }

  loadTeachers() {
    this.isLoading.set(true);
    this.api.getTeachers().subscribe({
      next: (res: TeacherDto[]) => {
        this.teachers.set(res);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }
}
