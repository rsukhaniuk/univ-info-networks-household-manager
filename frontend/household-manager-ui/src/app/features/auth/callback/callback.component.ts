import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { take } from 'rxjs';

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

  ngOnInit(): void {
    // Wait for auth state and redirect
    this.auth.appState$.pipe(take(1)).subscribe(appState => {
      const target = appState?.target || '/dashboard';
      this.router.navigate([target]);
    });

    // Fallback: if no appState after 3 seconds, redirect to dashboard
    setTimeout(() => {
      this.auth.isAuthenticated$.pipe(take(1)).subscribe(isAuth => {
        if (isAuth) {
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/']);
        }
      });
    }, 3000);
  }
}