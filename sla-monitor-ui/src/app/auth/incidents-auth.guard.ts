import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const incidentsAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);

  if (auth.isLoggedIn()) {
    return true;
  }

  auth.login('/incidents');
  return false;
};