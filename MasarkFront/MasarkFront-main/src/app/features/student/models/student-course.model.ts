import { StudentEntityId } from './student-id.model';

export interface StudentCourse {
  subjectId: StudentEntityId;
  subjectName: string;
  subjectArabicName: string;
  description?: string;
  teacherName?: string;
  gradeName?: string;
}
