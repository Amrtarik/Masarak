import { StudentEntityId } from './student-id.model';

export interface StudentScheduleSession {
  sessionId?: StudentEntityId;
  subjectId?: StudentEntityId;
  sessionTitle: string;
  sessionDate: string;
  duration: string | number;
  endsAt?: string;
  status?: string;
  subjectName?: string;
  className?: string;
  teacherName?: string;
  location?: string;
  meetingUrl?: string;
  description?: string;
}
