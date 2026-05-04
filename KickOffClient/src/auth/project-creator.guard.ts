import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { AuthStateService } from './services/auth-state.service';

export const projectCreatorGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const authState = inject(AuthStateService);
  const router = inject(Router);

  await authService.initialize();

  if (authState.canCreateProjects()) {
    return true;
  }

  router.navigate(['/landing']);
  return false;
};
