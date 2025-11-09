import { Injectable, inject } from '@angular/core';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private auth0 = inject(Auth0Service);

  // Observable properties
  isAuthenticated$ = this.auth0.isAuthenticated$;
  isLoading$ = this.auth0.isLoading$;
  user$ = this.auth0.user$;
  error$ = this.auth0.error$;
  
  /**
   * Get ID token claims
   */
  getIdToken$(): Observable<string | undefined> {
    return this.auth0.idTokenClaims$.pipe(
      map(claims => claims?.__raw)
    );
  }

  /**
   * Get access token silently
   */
  getAccessToken$(): Observable<string> {
    return this.auth0.getAccessTokenSilently();
  }

  /**
   * Get user ID from Auth0 (sub claim)
   */
  getUserId$(): Observable<string | undefined> {
    return this.user$.pipe(
      map(user => user?.sub)
    );
  }

  /**
   * Get user email
   */
  getUserEmail$(): Observable<string | undefined> {
    return this.user$.pipe(
      map(user => {
        const customEmail = user?.['https://householdmanager.com/email'];
        return customEmail || user?.email;
      })
    );
  }

  /**
   * Login with redirect
   */
  login(redirectPath: string = '/households'): void {
    this.auth0.loginWithRedirect({
      appState: { target: redirectPath, action: 'login' }
    });
  }

  /**
   * Signup (login with signup screen)
   */
  signup(): void {
    this.auth0.loginWithRedirect({
      authorizationParams: {
        screen_hint: 'signup'
      },
      appState: { target: '/households', action: 'signup' }
    });
  }

  /**
   * Logout
   */
  logout(): void {
    this.auth0.logout({
      logoutParams: {
        returnTo: window.location.origin
      }
    });
  }

  /**
   * Re-authenticate user with prompt=login
   * Used for sensitive operations like password change
   */
  reauthenticate(appState: any): void {
    this.auth0.loginWithRedirect({
      authorizationParams: {
        prompt: 'login', // Force user to re-enter credentials
        screen_hint: 'login'
      },
      appState: appState
    });
  }

  /**
   * Check if user has specific role
   */
  hasRole$(role: string): Observable<boolean> {
    return this.user$.pipe(
      map(user => {
        const roles = user?.['https://householdmanager.com/roles'] as string[];
        return roles?.includes(role) ?? false;
      })
    );
  }
  

  /**
   * Check if user is SystemAdmin
   */
  isSystemAdmin$(): Observable<boolean> {
    return this.hasRole$('SystemAdmin');
  }

  /**
   * Get user roles
   */
  getUserRoles$(): Observable<string[]> {
    return this.user$.pipe(
      map(user => {
        const roles = user?.['https://householdmanager.com/roles'] as string[];
        return roles || [];
      })
    );
  }
}