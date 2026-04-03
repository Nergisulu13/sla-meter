import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isAuthEndpoint =
    req.url.includes('/connect/token') ||
    req.url.includes('/connect/authorize') ||
    req.url.includes('/account/login') ||
    req.url.includes('/account/logout');

  const isProtectedRequest = req.url.includes('/api/');

  let authReq = req;
  const token = auth.getAccessToken();

  if (isProtectedRequest && !isAuthEndpoint && token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && isProtectedRequest) {
        const refreshToken = auth.getRefreshToken();
        const currentUrl = window.location.pathname || '/incidents';

        if (!refreshToken) {
          auth.clearTokens();
          auth.login(currentUrl, true);
          return throwError(() => error);
        }

        return auth.refreshToken().pipe(
          switchMap((res: any) => {
            if (!res?.access_token) {
              auth.clearTokens();
              auth.login(currentUrl, true);
              return throwError(() => error);
            }

            localStorage.setItem('access_token', res.access_token);

            if (res.refresh_token) {
              localStorage.setItem('refresh_token', res.refresh_token);
            }

            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${res.access_token}`
              }
            });

            return next(retryReq);
          }),
          catchError((refreshError) => {
            auth.clearTokens();
            auth.login(currentUrl, true);
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};