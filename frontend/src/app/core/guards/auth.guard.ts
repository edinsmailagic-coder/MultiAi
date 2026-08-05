import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

export const permissionGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  if (!auth.currentUser()) {
    try {
      await firstValueFrom(auth.loadCurrentUser());
    } catch {
      auth.logout();
      return router.createUrlTree(['/login']);
    }
  }

  const required = route.data['permission'] as string | string[] | undefined;
  if (!required) {
    return true;
  }

  const permissions = Array.isArray(required) ? required : [required];
  const allowed = permissions.some((permission) => auth.can(permission));
  return allowed ? true : router.createUrlTree(['/dashboard']);
};
