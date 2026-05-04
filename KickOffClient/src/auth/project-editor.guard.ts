import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from './services/auth.service';
import { AuthStateService } from './services/auth-state.service';
import { ProjectService } from '../app/services/project.service';

export const projectEditorGuard: CanActivateFn = async route => {
  const authService = inject(AuthService);
  const authState = inject(AuthStateService);
  const projectService = inject(ProjectService);
  const router = inject(Router);

  await authService.initialize();

  const currentUser = authState.currentUser();
  if (!currentUser) {
    router.navigate(['/landing']);
    return false;
  }

  const projectId = route.paramMap.get('id');
  if (!projectId) {
    router.navigate(['/landing']);
    return false;
  }

  try {
    const project = await firstValueFrom(projectService.getById(projectId));
    const isAdmin = (currentUser.roles ?? []).some(role => role.toLowerCase() === 'admin');

    if (isAdmin || project.ownerId === currentUser.id) {
      return true;
    }
  } catch {
  }

  router.navigate(['/project', projectId]);
  return false;
};
