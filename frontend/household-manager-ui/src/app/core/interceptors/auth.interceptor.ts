import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { switchMap, take, catchError } from 'rxjs';

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
      if (!token) {
        console.error('Auth token is empty!');
      }
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq);
    }),
    catchError(err => {
      console.error('Failed to get auth token:', err);
      // If we can't get token, the user is not authenticated
      // Let the request go through without token so API returns proper 401
      return next(req);
    })
  );
};