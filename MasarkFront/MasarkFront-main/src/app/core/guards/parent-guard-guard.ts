import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStateService } from '../services/auth-state-service';

export const parentGuard: CanActivateFn = () => {
  const auth   = inject(AuthStateService);
  const router = inject(Router);
  if (auth.isAuthenticated() && auth.isParent()) return true;
  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);
  return router.createUrlTree(['/dashboard']);
};