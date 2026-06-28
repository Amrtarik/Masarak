import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';

import {
  StartStudentExamResponse,
  StudentCourse,
  StudentEntityId,
  StudentExam,
  StudentExamAnswer,
  StudentExamGroup,
  StudentExamQuestion,
  StudentExamResult,
} from '../../models';
import { StudentService } from '../../services/student.service';

interface SelectedExam {
  subject: StudentCourse;
  exam: StudentExam;
}

@Component({
  selector: 'app-student-exams-page',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './exams.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentExamsPageComponent implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly groups = signal<StudentExamGroup[]>([]);
  readonly selectedExamId = signal<StudentEntityId | null>(null);
  readonly activeExam = signal<StartStudentExamResponse | null>(null);
  readonly examResult = signal<StudentExamResult | null>(null);
  readonly answers = signal<Record<string, StudentExamAnswer>>({});
  readonly isLoading = signal<boolean>(true);
  readonly isStarting = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);

  readonly totalExams = computed(() =>
    this.groups().reduce((total, group) => total + group.exams.length, 0),
  );

  readonly selectedExam = computed<SelectedExam | null>(() => {
    const selectedExamId = this.selectedExamId();

    for (const group of this.groups()) {
      const exam =
        group.exams.find((currentExam) => String(currentExam.examId) === String(selectedExamId)) ??
        null;

      if (exam) {
        return { subject: group.subject, exam };
      }
    }

    const firstGroup = this.groups().find((group) => group.exams.length > 0);
    const firstExam = firstGroup?.exams[0] ?? null;

    return firstGroup && firstExam ? { subject: firstGroup.subject, exam: firstExam } : null;
  });

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.actionMessage.set(null);

    this.studentService
      .getExamGroups()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (groups) => {
          this.groups.set(groups);
          this.selectedExamId.set(this.firstExamId(groups));
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          this.groups.set([]);
          this.selectedExamId.set(null);
          this.errorMessage.set(this.studentService.resolveErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  selectExam(exam: StudentExam): void {
    this.selectedExamId.set(exam.examId);
    this.activeExam.set(null);
    this.examResult.set(null);
    this.answers.set({});
    this.actionMessage.set(null);
  }

  startSelectedExam(): void {
    const selected = this.selectedExam();

    if (!selected) {
      return;
    }

    this.isStarting.set(true);
    this.actionMessage.set(null);
    this.examResult.set(null);

    this.studentService
      .startExam(selected.exam.examId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (exam) => {
          this.activeExam.set(exam);
          this.answers.set(this.createAnswerMap(exam.savedAnswers ?? []));
          this.isStarting.set(false);
        },
        error: (error: unknown) => {
          this.actionMessage.set(this.studentService.resolveErrorMessage(error));
          this.isStarting.set(false);
        },
      });
  }

  setTextAnswer(question: StudentExamQuestion, event: Event): void {
    const value = event.target instanceof HTMLTextAreaElement ? event.target.value : '';
    const key = this.answerKey(question.questionId);
    const currentAnswers = this.answers();

    this.answers.set({
      ...currentAnswers,
      [key]: {
        ...currentAnswers[key],
        questionId: question.questionId,
        answerText: value,
      },
    });
  }

  setSingleChoiceAnswer(question: StudentExamQuestion, choiceId: StudentEntityId): void {
    const key = this.answerKey(question.questionId);

    this.answers.set({
      ...this.answers(),
      [key]: {
        questionId: question.questionId,
        selectedOptionId: String(choiceId),
        selectedChoiceIds: [choiceId],
      },
    });
  }

  toggleMultipleChoiceAnswer(
    question: StudentExamQuestion,
    choiceId: StudentEntityId,
    event: Event,
  ): void {
    const checked = event.target instanceof HTMLInputElement ? event.target.checked : false;
    const key = this.answerKey(question.questionId);
    const currentAnswers = this.answers();
    const currentChoiceIds = currentAnswers[key]?.selectedChoiceIds ?? [];
    const nextChoiceIds = checked
      ? [...currentChoiceIds, choiceId]
      : currentChoiceIds.filter((currentChoiceId) => String(currentChoiceId) !== String(choiceId));

    this.answers.set({
      ...currentAnswers,
      [key]: {
        questionId: question.questionId,
        selectedOptionId: nextChoiceIds.map(String).join(','),
        selectedChoiceIds: nextChoiceIds,
      },
    });
  }

  setFileAnswer(question: StudentExamQuestion, event: Event): void {
    const file = event.target instanceof HTMLInputElement ? event.target.files?.[0] ?? null : null;
    const key = this.answerKey(question.questionId);
    const currentAnswers = this.answers();

    this.answers.set({
      ...currentAnswers,
      [key]: {
        ...currentAnswers[key],
        questionId: question.questionId,
        file,
      },
    });
  }

  saveAnswers(): void {
    const activeExam = this.activeExam();

    if (!activeExam) {
      return;
    }

    this.isSaving.set(true);
    this.actionMessage.set(null);

    this.studentService
      .saveExamAnswers(activeExam.studentExamId, { answers: this.answerList() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.actionMessage.set('Answers saved.');
        },
        error: (error: unknown) => {
          this.isSaving.set(false);
          this.actionMessage.set(this.studentService.resolveErrorMessage(error));
        },
      });
  }

  submitActiveExam(): void {
    const activeExam = this.activeExam();

    if (!activeExam) {
      return;
    }

    this.isSubmitting.set(true);
    this.actionMessage.set(null);

    this.studentService
      .saveExamAnswers(activeExam.studentExamId, { answers: this.answerList() })
      .pipe(
        switchMap(() => this.studentService.submitExam(activeExam.studentExamId)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.examResult.set(result);
          this.activeExam.set(null);
          this.answers.set({});
          this.isSubmitting.set(false);
          this.actionMessage.set('Exam submitted successfully.');
        },
        error: (error: unknown) => {
          this.isSubmitting.set(false);
          this.actionMessage.set(this.studentService.resolveErrorMessage(error));
        },
      });
  }

  isChoiceQuestion(question: StudentExamQuestion): boolean {
    return (question.choices?.length ?? 0) > 0 && !this.isTextQuestion(question);
  }

  isMultipleChoiceQuestion(question: StudentExamQuestion): boolean {
    return (question.questionType ?? '').toLowerCase().includes('multiple');
  }

  isChoiceSelected(questionId: StudentEntityId, choiceId: StudentEntityId): boolean {
    const selectedChoiceIds = this.answers()[this.answerKey(questionId)]?.selectedChoiceIds ?? [];
    return selectedChoiceIds.some(
      (selectedChoiceId) => String(selectedChoiceId) === String(choiceId),
    );
  }

  textAnswer(questionId: StudentEntityId): string {
    return this.answers()[this.answerKey(questionId)]?.answerText ?? '';
  }

  fileAnswerName(questionId: StudentEntityId): string | null {
    return this.answers()[this.answerKey(questionId)]?.file?.name ?? null;
  }

  isFileQuestion(question: StudentExamQuestion): boolean {
    return (question.questionType ?? '').toLowerCase().includes('file');
  }

  private answerList(): StudentExamAnswer[] {
    return Object.values(this.answers());
  }

  private isTextQuestion(question: StudentExamQuestion): boolean {
    const questionType = (question.questionType ?? '').toLowerCase();
    return (
      questionType.includes('text') ||
      questionType.includes('short') ||
      questionType.includes('essay') ||
      questionType.includes('fill')
    );
  }

  private createAnswerMap(answers: StudentExamAnswer[]): Record<string, StudentExamAnswer> {
    return answers.reduce<Record<string, StudentExamAnswer>>(
      (answerMap, answer) => ({
        ...answerMap,
        [this.answerKey(answer.questionId)]: answer,
      }),
      {},
    );
  }

  private answerKey(id: StudentEntityId): string {
    return String(id);
  }

  private firstExamId(groups: StudentExamGroup[]): StudentEntityId | null {
    for (const group of groups) {
      const examId = group.exams[0]?.examId;

      if (examId !== undefined) {
        return examId;
      }
    }

    return null;
  }
}
