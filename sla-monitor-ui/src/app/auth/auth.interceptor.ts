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
  const currentUrl = window.location.pathname || '/incidents';

  if (!isProtectedRequest || isAuthEndpoint) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const refreshToken = auth.getRefreshToken();

  if (token && auth.isTokenExpired(token)) {
    if (!refreshToken) {
      auth.forceLogin(currentUrl);
      return throwError(() => new Error('No refresh token'));
    }

    return auth.refreshToken().pipe(
      switchMap((res: any) => {
        if (!res?.access_token) {
          auth.forceLogin(currentUrl);
          return throwError(() => new Error('Refresh failed'));
        }

        localStorage.setItem('access_token', res.access_token);

        if (res.refresh_token) {
          localStorage.setItem('refresh_token', res.refresh_token);
        }

        const refreshedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${res.access_token}`
          }
        });

        return next(refreshedReq);
      }),
      catchError((refreshError) => {
        auth.forceLogin(currentUrl);
        return throwError(() => refreshError);
      })
    );
  }

  let authReq = req;

  if (token && !auth.isTokenExpired(token)) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      const latestRefreshToken = auth.getRefreshToken();

      if (!latestRefreshToken) {
        auth.forceLogin(currentUrl);
        return throwError(() => error);
      }

      return auth.refreshToken().pipe(
        switchMap((res: any) => {
          if (!res?.access_token) {
            auth.forceLogin(currentUrl);
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
          auth.forceLogin(currentUrl);
          return throwError(() => refreshError);
        })
      );
    })
  );
};