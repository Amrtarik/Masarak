import { StudentEntityId } from './student-id.model';

export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused' | string;

export interface StudentAttendanceRecord {
  attendanceId?: StudentEntityId;
  sessionId?: StudentEntityId;
  subjectId?: StudentEntityId;
  subjectName?: string;
  sessionTitle?: string;
  sessionDate: string;
  status: AttendanceStatus;
  notes?: string;
}

export interface StudentSubjectAttendance {
  subjectId: StudentEntityId;
  subjectName: string;
  totalSessions: number;
  presentCount: number;
  absentCount: number;
  excusedCount: number;
  attendancePercentage: number;
}

export interface StudentAttendanceSummary {
  attendanceRate: number;
  presentCount: number;
  absentCount: number;
  lateCount?: number;
  excusedCount?: number;
  totalSessions?: number;
  subjects?: StudentSubjectAttendance[];
  records?: StudentAttendanceRecord[];
}
