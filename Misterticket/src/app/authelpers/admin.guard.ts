import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Dashboard access: Admin and Organizer only.
 * The API enforces the same rule; this only avoids showing a page that would 403.
 */
export const adminGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const role = authService.currentUser()?.role;

  if (role === 'Admin' || role === 'Organizer') {
    return true;
  }

  // Not signed in at all: offer the login page. Signed in as a Client: send home.
  return authService.isLoggedIn()
    ? router.createUrlTree(['/'])
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
