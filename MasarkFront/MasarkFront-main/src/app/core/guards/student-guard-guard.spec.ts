import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { studentGuardGuard } from './student-guard-guard';

describe('studentGuardGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => studentGuardGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
