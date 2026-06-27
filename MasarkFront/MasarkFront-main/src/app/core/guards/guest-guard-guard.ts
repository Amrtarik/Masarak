import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStateService } from '../services/auth-state-service';

/** Prevents authenticated users from reaching login/register */
export const guestGuard: CanActivateFn = () => {
  const auth   = inject(AuthStateService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return true;
  return router.createUrlTree(['/dashboard']);
};