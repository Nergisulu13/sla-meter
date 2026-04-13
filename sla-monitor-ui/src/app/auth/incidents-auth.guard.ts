import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const incidentsAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);

  if (auth.hasAccessToken() || auth.hasRefreshToken()) {
    return true;
  }

  auth.forceLogin('/incidents');
  return false;
};