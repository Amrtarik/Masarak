import { StudentCourse } from './student-course.model';
import { StudentEntityId } from './student-id.model';

export type StudentExamStatus =
  | 'Available'
  | 'Started'
  | 'Submitted'
  | 'Completed'
  | 'Expired'
  | string;

export type StudentQuestionType = 'SingleChoice' | 'MultipleChoice' | 'Text' | string;

export interface StudentExam {
  examId: StudentEntityId;
  subjectId?: StudentEntityId;
  title: string;
  description?: string;
  startsAt?: string;
  endsAt?: string;
  durationMinutes?: number;
  questionCount?: number;
  maxScore?: number;
  status?: StudentExamStatus;
}

export interface StudentExamGroup {
  subject: StudentCourse;
  exams: StudentExam[];
}

export interface StudentExamChoice {
  choiceId: StudentEntityId;
  label?: string;
  text: string;
}

export interface StudentExamQuestion {
  questionId: StudentEntityId;
  text: string;
  questionType: StudentQuestionType;
  points?: number;
  choices?: StudentExamChoice[];
}

export interface StartStudentExamResponse {
  studentExamId: StudentEntityId;
  examId: StudentEntityId;
  title?: string;
  startedAt: string;
  expiresAt?: string;
  secondsRemaining?: number;
  questions: StudentExamQuestion[];
  savedAnswers?: StudentExamAnswer[];
}

export interface StudentExamAnswer {
  questionId: StudentEntityId;
  answerText?: string;
  selectedChoiceIds?: StudentEntityId[];
  selectedOptionId?: string;
  file?: File | null;
}

export interface SaveStudentExamAnswersRequest {
  answers: StudentExamAnswer[];
}

export interface SaveStudentExamAnswersResponse {
  studentExamId: StudentEntityId;
  savedAt?: string;
  message?: string;
}

export interface StudentExamResult {
  studentExamId: StudentEntityId;
  examId?: StudentEntityId;
  score: number;
  maxScore: number;
  percentage: number;
  gradeLetter?: string;
  passed?: boolean;
  submittedAt?: string;
  hasPendingManualGrading?: boolean;
}

export type SubmitStudentExamResponse = StudentExamResult;
