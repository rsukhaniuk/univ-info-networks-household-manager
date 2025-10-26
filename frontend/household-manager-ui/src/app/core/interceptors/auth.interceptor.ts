import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { switchMap, take, catchError, throwError } from 'rxjs';

/**
 * Auth interceptor - adds JWT token to API requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Skip for non-API requests
  if (!req.url.includes('/api/')) {
    return next(req);
  }

  return auth.getAccessTokenSilently().pipe(
    take(1),
    switchMap(token => {
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq);
    }),
    catchError(err => {
      console.error('Auth interceptor error:', err);
      return next(req);
    })
  );
};