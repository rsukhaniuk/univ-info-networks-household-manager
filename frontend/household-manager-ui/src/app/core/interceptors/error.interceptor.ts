import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ServerErrorService } from '../services/server-error.service';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';


export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const serverErrorService = inject(ServerErrorService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unknown error occurred';
      let errorDetails: any = null;
      const extracted: string[] = [];

      // Don't show errors for Auth0 or other internal requests
      const isInternalRequest = 
        req.url.includes('auth0.com') || 
        req.url.includes('/oauth/') ||
        req.url.includes('/token');

      // Don't show 401 errors (handled by Auth0)
      const shouldSkipError = isInternalRequest || error.status === 401;

      if (error.error instanceof ErrorEvent) {
        // Client-side error (network issue, etc.)
        errorMessage = `Client Error: ${error.error.message}`;
        extracted.push(errorMessage);
        
      } else {
        // Server-side error
        let payload: any = error.error;

        // Try to parse string payloads as JSON
        if (typeof payload === 'string') {
          const trimmed = payload.trim();
          if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
            try {
              payload = JSON.parse(trimmed);
            } catch {
              // Keep original string if parse fails
            }
          }
        }

        if (payload && typeof payload === 'object') {
          if (payload.errors && typeof payload.errors === 'object') {
            errorDetails = payload;
            Object.keys(payload.errors).forEach(field => {
              const messages = payload.errors[field];
              if (Array.isArray(messages)) {
                messages.forEach(msg => {
                  // Clean up technical ASP.NET Core error messages
                  const cleanedMsg = cleanErrorMessage(msg);
                  extracted.push(cleanedMsg);
                });
              }
            });
          }

          if (extracted.length === 0 && payload.detail && typeof payload.detail === 'string') {
            extracted.push(payload.detail);
            errorDetails = payload;
          }

          if (extracted.length === 0 && payload.title && typeof payload.title === 'string') {
            extracted.push(payload.title);
            errorDetails = payload;
          }
        } else if (typeof payload === 'string' && payload.trim()) {
          extracted.push(payload.trim());
        }

        if (extracted.length === 0) {
          extracted.push(getStatusMessage(error.status));
        }

        if (!shouldSkipError) {
          serverErrorService.setErrors(extracted);
        }

        errorMessage = extracted[0];

        if (!environment.production && !shouldSkipError) {
          console.error('[HTTP Error]', {
            status: error.status,
            statusText: error.statusText,
            message: errorMessage,
            allMessages: extracted,
            details: errorDetails,
            url: req.url
          });
        }
      }

      return throwError(() => ({
        status: error.status,
        message: errorMessage,
        messages: extracted, // All error messages
        details: errorDetails,
        originalError: error
      }));
    })
  );
};

/**
 * Get user-friendly message based on HTTP status code
 */
function getStatusMessage(status: number): string {
  switch (status) {
    case 0:
      return 'Unable to connect to server. Please check your internet connection.';
    case 400:
      return 'Invalid request. Please check your input.';
    case 401:
      return 'You are not authorized. Please log in.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested resource was not found.';
    case 409:
      return 'Conflict detected. The resource may have been modified.';
    case 422:
      return 'Validation failed. Please check your input.';
    case 500:
      return 'Server error. Please try again later.';
    case 502:
      return 'Bad gateway. The server is temporarily unavailable.';
    case 503:
      return 'Service unavailable. Please try again later.';
    default:
      return `An error occurred (Status: ${status}). Please try again.`;
  }
}

function cleanErrorMessage(message: string): string {
  if (message.includes('could not be converted to System.Guid')) {
    return 'Invalid invite code format. Please check and try again.';
  }
  
  if (message.includes('could not be converted to System.Int32')) {
    return 'Invalid number format. Please enter a valid number.';
  }
  
  if (message.includes('could not be converted to System.DateTime')) {
    return 'Invalid date format. Please select a valid date.';
  }
  
  if (message.includes('could not be converted to System.')) {
    return 'Invalid data format. Please check your input.';
  }
  
  return message;
}