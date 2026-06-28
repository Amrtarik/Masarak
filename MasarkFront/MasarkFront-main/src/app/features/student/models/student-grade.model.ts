import { StudentEntityId } from './student-id.model';

export interface StudentGrade {
  subjectId?: StudentEntityId;
  subjectName: string;
  gradeLetter: string;
  finalGrade: number;
  termName?: string;
  academicYear?: string;
  examAverage?: number;
  assignmentAverage?: number;
}
