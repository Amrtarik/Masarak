import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TeacherAssignments } from './teacher-assignments';

describe('TeacherAssignments', () => {
  let component: TeacherAssignments;
  let fixture: ComponentFixture<TeacherAssignments>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeacherAssignments],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherAssignments);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
