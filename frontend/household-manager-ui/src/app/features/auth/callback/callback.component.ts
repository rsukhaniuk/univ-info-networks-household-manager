import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { take } from 'rxjs';
import { UserService } from '../../profile/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { RequestPasswordChangeRequest } from '../../../core/models/user.model';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container d-flex justify-content-center align-items-center vh-100 bg-light">
      <div class="text-center">
        <div class="spinner-border text-primary mb-3" role="status" style="width: 3rem; height: 3rem;">
          <span class="visually-hidden">Loading...</span>
        </div>
        <h3 class="mb-2">Signing you in...</h3>
        <p class="text-muted">Please wait while we complete your authentication</p>
      </div>
    </div>
  `,
  styles: [`
    .callback-container {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    }

    .spinner-border {
      animation: spinner-border 0.75s linear infinite;
    }

    h3 {
      color: #0d6efd;
      font-weight: 600;
    }
  `]
})
export class CallbackComponent implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private userService = inject(UserService);
  private toastService = inject(ToastService);

  ngOnInit(): void {
    let redirectHandled = false;

    // Wait for auth state and redirect
    this.auth.appState$.pipe(take(1)).subscribe(appState => {
      // Check if this is a password change action after re-authentication
      if (appState?.action === 'password-change') {
        redirectHandled = true;
        this.handlePasswordChange(appState.resultUrl);
        return;
      }

      // Show success message based on action
      if (appState?.action === 'signup') {
        this.toastService.success('Welcome! Your account has been created successfully!');
      } else if (appState?.action === 'login') {
        this.toastService.success('Successfully logged in!');
      }

      // Default redirect behavior
      const target = appState?.target || '/households';
      redirectHandled = true;
      this.router.navigate([target]);
    });

    // Fallback: if no appState after 3 seconds, redirect to households
    setTimeout(() => {
      if (!redirectHandled) {
        this.auth.isAuthenticated$.pipe(take(1)).subscribe(isAuth => {
          if (isAuth) {
            this.router.navigate(['/households']);
          } else {
            this.router.navigate(['/']);
          }
        });
      }
    }, 3000);
  }

  private handlePasswordChange(resultUrl: string): void {
    this.toastService.info('Re-authentication successful. Redirecting to password change...', 3000);

    const request: RequestPasswordChangeRequest = {
      resultUrl: resultUrl
    };

    this.userService.requestPasswordChange(request).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Set flag before redirecting to Auth0
          sessionStorage.setItem('passwordChangeInProgress', 'true');

          // Redirect to Auth0 hosted password change page
          window.location.href = response.data.ticketUrl;
        }
      },
      error: () => {
        // Error will be shown by error interceptor
        // Redirect back to profile on error
        this.router.navigate(['/profile']);
      }
    });
  }
}