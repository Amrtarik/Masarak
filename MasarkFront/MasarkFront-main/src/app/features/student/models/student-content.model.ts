import { StudentCourse } from './student-course.model';
import { StudentEntityId } from './student-id.model';

export type StudentContentType = 'Pdf' | 'Video' | 'Resource' | 'Link' | 'File' | string;

export interface StudentContentItem {
  contentId: StudentEntityId;
  subjectId?: StudentEntityId;
  title: string;
  contentType: StudentContentType;
  sourceType?: string;
  description?: string;
  fileUrl?: string;
  url?: string;
  fileSizeBytes?: number | null;
  createdAt?: string;
  isActive?: boolean;
}

export interface StudentContentGroup {
  subject: StudentCourse;
  items: StudentContentItem[];
}
