import { StudentEntityId } from './student-id.model';

export interface StudentProfile {
  studentId?: StudentEntityId;
  fullName: string;
  email: string;
  academicStatus: string;
  gradeName: string;
}
