import { StudentEntityId } from './student-id.model';

export interface StudentClass {
  classId?: StudentEntityId;
  className: string;
  gradeName?: string;
  academicYear?: number;
}
