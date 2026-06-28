import { StudentEntityId } from './student-id.model';

export interface StudentPerformance {
  subjectId?: StudentEntityId;
  subjectName: string;
  finalGrade: number;
  gradeLetter: string;
  avgExam: number;
  avgAssignment: number;
}
