import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * Error interceptor - handles HTTP errors globally
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unknown error occurred';
      let errorDetails: any = null;

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Client Error: ${error.error.message}`;
      } else {
        // Server-side error
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (typeof error.error === 'string') {
          errorMessage = error.error;
        } else {
          errorMessage = `Server Error: ${error.status} - ${error.message}`;
        }

        // Extract validation errors if available
        if (error.error?.errors) {
          errorDetails = error.error.errors;
        }

        // Handle specific status codes
        switch (error.status) {
          case 401:
            // Unauthorized - redirect to login
            console.error('Unauthorized - redirecting to login');
            router.navigate(['/']);
            break;

          case 403:
            // Forbidden
            console.error('Access denied:', errorMessage);
            break;

          case 404:
            // Not found
            console.error('Resource not found:', errorMessage);
            break;

          case 422:
            // Validation error
            console.error('Validation failed:', errorDetails || errorMessage);
            break;

          case 500:
            // Server error
            console.error('Server error:', errorMessage);
            break;
        }
      }

      console.error('HTTP Error:', {
        status: error.status,
        message: errorMessage,
        details: errorDetails,
        url: req.url
      });

      return throwError(() => ({
        status: error.status,
        message: errorMessage,
        details: errorDetails,
        originalError: error
      }));
    })
  );
};