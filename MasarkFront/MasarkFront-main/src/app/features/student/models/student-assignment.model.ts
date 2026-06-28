import { StudentCourse } from './student-course.model';
import { StudentEntityId } from './student-id.model';

export type StudentAssignmentStatus =
  | 'Pending'
  | 'Submitted'
  | 'Graded'
  | 'Late'
  | 'Missing'
  | string;

export interface StudentAssignment {
  assignmentId: StudentEntityId;
  subjectId?: StudentEntityId;
  title: string;
  description?: string;
  dueDate?: string;
  maxScore?: number;
  score?: number;
  status?: StudentAssignmentStatus;
  submittedAt?: string;
  attachmentUrl?: string;
}

export interface StudentAssignmentGroup {
  subject: StudentCourse;
  assignments: StudentAssignment[];
}

export interface SubmitAssignmentRequest {
  textContent?: string;
  file?: File | null;
}

export interface StudentAssignmentSubmission {
  submissionId?: StudentEntityId;
  assignmentId: StudentEntityId;
  submittedAt: string;
  status: StudentAssignmentStatus;
  score?: number | null;
  feedback?: string | null;
  fileUrl?: string | null;
}
