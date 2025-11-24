import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { switchMap, take, catchError, EMPTY } from 'rxjs';

/**
 * Auth interceptor - adds JWT token to API requests
 * Handles token refresh and session expiration
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
      const authErrors = [
        'login_required',
        'consent_required',
        'interaction_required',
        'missing_refresh_token',  // Refresh token expired or missing
        'invalid_grant'           // Token is no longer valid
      ];
      const code = err.error || err.error_description || err.code;
      const errorMessage = err.message || '';
      const isAuthError = (code && authErrors.includes(code)) ||
                          errorMessage.includes('Missing Refresh Token');

      if (isAuthError) {
        console.warn('[Auth] Session expired or refresh token missing, logging out...', code || errorMessage);
        auth.logout({
          logoutParams: {
            returnTo: window.location.origin
          }
        });
        return EMPTY;
      }

      // Network or other error - let request through, API will handle it
      console.error('Failed to get auth token:', err);
      return next(req);
    })
  );
};