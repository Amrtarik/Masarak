import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Session {
  title: string;
  subject: string;
  grade: string;
  time: string;
  status: 'live' | 'upcoming' | 'done';
}

interface RecentActivity {
  icon: string;
  text: string;
  time: string;
  type: 'submission' | 'alert' | 'message';
}

@Component({
  selector: 'app-teacher',
  imports: [CommonModule],
  templateUrl: './teacher.html',
  styleUrl: './teacher.css',
})
export class TeacherComponent {
  readonly today = new Date().toLocaleDateString('ar-EG', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  readonly sessions = signal<Session[]>([
    { title: 'الرياضيات', subject: 'جبر الصف الخامس', grade: 'الصف ٥أ', time: '٠٨:٠٠ – ٠٨:٤٥', status: 'live' },
    { title: 'العلوم', subject: 'التحولات الفيزيائية', grade: 'الصف ٤ب', time: '٠٩:٠٠ – ٠٩:٤٥', status: 'upcoming' },
    { title: 'اللغة العربية', subject: 'النحو والصرف', grade: 'الصف ٥ج', time: '١٠:٠٠ – ١٠:٤٥', status: 'upcoming' },
    { title: 'الرياضيات', subject: 'الهندسة الفراغية', grade: 'الصف ٦أ', time: '١١:٠٠ – ١١:٤٥', status: 'done' },
  ]);

  readonly activities = signal<RecentActivity[]>([
    { icon: '📝', text: 'تم تقديم واجب الرياضيات من طالب: أحمد علي', time: 'منذ ١٠ دقائق', type: 'submission' },
    { icon: '📢', text: 'تذكير: اجتماع المعلمين غداً الساعة ١٠:٠٠ ص', time: 'منذ ٣٠ دقيقة', type: 'alert' },
    { icon: '💬', text: 'رسالة جديدة من ولي أمر الطالب سارة محمد', time: 'منذ ساعة', type: 'message' },
    { icon: '📝', text: 'تم تقديم واجب العلوم من طالبة: نور أحمد', time: 'منذ ساعتين', type: 'submission' },
  ]);

  readonly stats = signal([
    { label: 'الواجبات المعلقة', value: '٣٥', icon: '📋', color: '#4f8cd4', bg: '#eff6ff' },
    { label: 'إجمالي الطلاب', value: '١٢٤', icon: '🎓', color: '#059669', bg: '#ecfdf5' },
    { label: 'الدروس هذا الأسبوع', value: '١٨', icon: '📚', color: '#7c3aed', bg: '#f5f3ff' },
    { label: 'متوسط الحضور', value: '٩٢٪', icon: '✅', color: '#d97706', bg: '#fffbeb' },
  ]);

  sessionStatusLabel(status: Session['status']): string {
    return { live: 'جارية الآن', upcoming: 'قادمة', done: 'منتهية' }[status];
  }
}
