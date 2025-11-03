import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from './services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import {
  UserProfileDto,
  UpdateProfileRequest,
  ConnectionInfo,
  RequestPasswordChangeRequest,
  ChangeEmailRequest
} from '../../core/models/user.model';
import { UtcDatePipe } from '../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, UtcDatePipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private toastService = inject(ToastService);

  // Data
  profile: UserProfileDto | null = null;
  auth0User$ = this.authService.user$;
  connectionInfo: ConnectionInfo | null = null;

  // State
  isLoading = true;
  isSubmitting = false;
  isChangingPassword = false;
  isChangingEmail = false;

  // Forms
  profileForm: FormGroup;
  emailForm: FormGroup;

  constructor() {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.maxLength(50)]]
    });

    this.emailForm = this.fb.group({
      newEmail: ['', [Validators.required, Validators.email, Validators.maxLength(255)]]
    });
  }

  ngOnInit(): void {
    this.loadProfile();
    this.loadConnectionInfo();
    this.checkPasswordChangeSuccess();
  }

  /**
   * Check if user was redirected back from Auth0 password change
   * Shows success message and prompts to re-login
   */
  checkPasswordChangeSuccess(): void {
    // Check if there's a flag in sessionStorage indicating password was just changed
    const passwordChanged = sessionStorage.getItem('passwordChangeInProgress');

    if (passwordChanged === 'true') {
      sessionStorage.removeItem('passwordChangeInProgress');
      this.toastService.success('Password updated successfully! Please sign in again to refresh your session.', 5000);

      // Auto-logout after 5 seconds
      setTimeout(() => {
        this.authService.logout();
      }, 5000);
    }
  }

  loadProfile(): void {
    this.isLoading = true;

    this.userService.getMyProfile().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.profile = response.data;
          
          // Populate form
          this.profileForm.patchValue({
            firstName: response.data.user.firstName || '',
            lastName: response.data.user.lastName || ''
          });
        }
        this.isLoading = false;
      },
      error: () => {
        // Error will be shown in global error banner by error interceptor
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.profileForm.invalid || this.isSubmitting) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    const request: UpdateProfileRequest = {
      firstName: this.profileForm.value.firstName || undefined,
      lastName: this.profileForm.value.lastName || undefined
    };

    this.userService.updateMyProfile(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Profile updated successfully');
          this.loadProfile(); // Reload to get updated data
        }
        this.isSubmitting = false;
      },
      error: (error) => {
        // Error will be shown in global error banner by error interceptor
        this.isSubmitting = false;
      }
    });
  }

  setCurrentHousehold(householdId: string): void {
    this.userService.setCurrentHousehold({ householdId }).subscribe({
      next: () => {
        this.toastService.success('Current household updated successfully');
        this.loadProfile();
      },
      error: (error) => {
        // Error will be shown in global error banner by error interceptor
      }
    });
  }

  clearCurrentHousehold(): void {
    this.userService.setCurrentHousehold({ householdId: undefined }).subscribe({
      next: () => {
        this.toastService.success('Current household cleared');
        this.loadProfile();
      },
      error: (error) => {
        // Error will be shown in global error banner by error interceptor
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  // Account Management Methods

  loadConnectionInfo(): void {
    this.userService.getConnectionInfo().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.connectionInfo = response.data;
        }
      },
      error: (error) => {
        console.error('Failed to load connection info:', error);
      }
    });
  }

  onChangePassword(): void {
    if (this.isChangingPassword || !this.connectionInfo?.canChangePassword) {
      return;
    }

    this.isChangingPassword = true;

    const request: RequestPasswordChangeRequest = {
      resultUrl: window.location.origin + '/profile'
    };

    this.userService.requestPasswordChange(request).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Set flag before redirecting to Auth0
          sessionStorage.setItem('passwordChangeInProgress', 'true');

          // Redirect to Auth0 hosted password change page
          window.location.href = response.data.ticketUrl;
        }
        this.isChangingPassword = false;
      },
      error: (error) => {
        // Error will be shown in global error banner by error interceptor
        this.isChangingPassword = false;
      }
    });
  }

  onChangeEmail(): void {
    if (this.emailForm.invalid || this.isChangingEmail || !this.connectionInfo?.canChangeEmail) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isChangingEmail = true;

    const request: ChangeEmailRequest = {
      newEmail: this.emailForm.value.newEmail,
      verifyEmail: false // Admin operation, no verification
    };

    this.userService.changeEmail(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Email changed successfully! You will be logged out in 3 seconds to refresh your session...', 5000);
          this.emailForm.reset();

          // Logout after 3 seconds to force re-login with new email
          setTimeout(() => {
            this.authService.logout();
          }, 3000);
        }
        this.isChangingEmail = false;
      },
      error: (error) => {
        console.error('Email change error:', error);
        // Error will be shown in global error banner by error interceptor
        this.isChangingEmail = false;
      }
    });
  }

  get firstName() { return this.profileForm.get('firstName'); }
  get lastName() { return this.profileForm.get('lastName'); }
  get newEmail() { return this.emailForm.get('newEmail'); }
}