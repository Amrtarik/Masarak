import { StudentAttendanceSummary } from './student-attendance.model';
import { StudentClass } from './student-class.model';
import { StudentCourse } from './student-course.model';
import { StudentPerformance } from './student-performance.model';
import { StudentProfile } from './student-profile.model';
import { StudentScheduleSession } from './student-schedule.model';

export interface StudentDashboardData {
  profile: StudentProfile;
  classInfo: StudentClass;
  courses: StudentCourse[];
  schedule: StudentScheduleSession[];
  attendance: StudentAttendanceSummary;
  performance: StudentPerformance[];
}
