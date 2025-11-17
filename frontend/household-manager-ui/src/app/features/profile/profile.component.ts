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
  ChangeEmailRequest,
  AccountDeletionCheckResult
} from '../../core/models/user.model';
import { UtcDatePipe } from '../../shared/pipes/utc-date.pipe';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, UtcDatePipe, ConfirmationDialogComponent, FormsModule],
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

  // Account deletion
  showDeleteDialog = false;
  isDeletingAccount = false;
  deletionCheck: AccountDeletionCheckResult | null = null;
  emailConfirmation = '';
  deleteDialogData: ConfirmDialogData | null = null;

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

    // Check if user was redirected back after re-authentication for account deletion
    const pendingDeletion = sessionStorage.getItem('pendingAccountDeletion');
    if (pendingDeletion === 'true') {
      sessionStorage.removeItem('pendingAccountDeletion');
      this.isDeletingAccount = true;

      // Proceed with account deletion
      this.userService.deleteAccount().subscribe({
        next: () => {
          this.toastService.success('Account deleted successfully. Goodbye!', 3000);

          // Logout and redirect
          setTimeout(() => {
            this.authService.logout();
          }, 2000);
        },
        error: (error) => {
          this.isDeletingAccount = false;
          this.toastService.error(error?.error?.detail || 'Failed to delete account');
        }
      });
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
      error: () => {
        // Error will be shown by error interceptor
      }
    });
  }

  onChangePassword(): void {
    if (this.isChangingPassword || !this.connectionInfo?.canChangePassword) {
      return;
    }

    this.isChangingPassword = true;

    // Force re-authentication before allowing password change
    // This ensures the user proves they know their current credentials
    this.authService.reauthenticate({
      target: '/profile',
      action: 'password-change',
      resultUrl: window.location.origin + '/profile'
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
      error: () => {
        // Error will be shown by error interceptor
        this.isChangingEmail = false;
      }
    });
  }

  // ========================================
  // Account Deletion
  // ========================================

  /**
   * Open delete account dialog
   * First checks if user can delete account
   */
  openDeleteAccountDialog(): void {
    this.userService.canDeleteAccount().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.deletionCheck = response.data;

          // Always show dialog with full information
          this.deleteDialogData = {
            title: response.data.canDelete ? 'Delete Account' : 'Cannot Delete Account',
            message: this.getDeleteWarningMessage(response.data),
            confirmText: response.data.canDelete ? 'Continue' : 'OK',
            cancelText: 'Cancel',
            confirmClass: 'danger',
            icon: 'fa-exclamation-triangle',
            iconClass: 'text-danger'
          };

          this.showDeleteDialog = true;
          this.emailConfirmation = '';
        }
      },
      error: () => {
        this.toastService.error('Failed to check account deletion status');
      }
    });
  }

  /**
   * Generate warning message with household and task info
   */
  private getDeleteWarningMessage(check: AccountDeletionCheckResult): string {
    if (!check.canDelete) {
      // Show why deletion is blocked
      let message = '❌ CANNOT DELETE ACCOUNT\n\n';
      message += 'Your account cannot be deleted at this time for the following reason:\n\n';

      if (check.message) {
        message += `${check.message}\n\n`;
      }

      message += '📋 Account Deletion Requirements:\n\n';
      message += '✓ You must NOT be the sole owner of any household\n';
      message += '  → Transfer ownership or add another owner before leaving\n\n';

      if (check.ownedHouseholdsCount > 0 && check.ownedHouseholdNames.length > 0) {
        message += `⚠️ You are currently the sole owner of ${check.ownedHouseholdsCount} household(s):\n`;
        check.ownedHouseholdNames.forEach(name => {
          message += `  • ${name}\n`;
        });
        message += '\n';
      }

      message += '📊 Current Status:\n';
      message += `• Member of ${check.memberHouseholdsCount} household(s)\n`;
      message += `• ${check.assignedTasksCount} task(s) assigned to you\n`;

      return message;
    }

    // Can delete - show warning
    let message = '⚠️ WARNING: This action cannot be undone!\n\n';
    message += 'Your account will be permanently deleted.\n\n';

    // Warning about sole-owner households that will be deleted
    if (check.ownedHouseholdsCount > 0 && check.ownedHouseholdNames.length > 0) {
      message += `🗑️ ${check.ownedHouseholdsCount} household(s) where you are the sole owner will be PERMANENTLY DELETED:\n`;
      check.ownedHouseholdNames.forEach(name => {
        message += `  • ${name}\n`;
      });
      message += '\n';
    }

    message += '📊 What will happen:\n';
    message += `• You will be removed from ${check.memberHouseholdsCount} household(s)\n`;
    message += `• Your ${check.assignedTasksCount} assigned task(s) will be reassigned or marked as unassigned\n\n`;
    message += `Please type your email (${this.profile?.user.email}) to confirm:`;
    return message;
  }

  /**
   * Check if email confirmation matches
   */
  get isEmailConfirmed(): boolean {
    return this.emailConfirmation === this.profile?.user.email;
  }

  /**
   * Handle delete dialog confirmation
   * Triggers Auth0 re-authentication then deletes account
   */
  onDeleteConfirmed(): void {
    if (!this.isEmailConfirmed) {
      this.toastService.error('Email does not match');
      return;
    }

    this.showDeleteDialog = false;

    // Store delete flag in sessionStorage to continue after re-auth
    sessionStorage.setItem('pendingAccountDeletion', 'true');

    // Trigger Auth0 re-authentication with redirect
    this.authService.reauthenticate({ target: '/profile' });
  }

  /**
   * Handle delete dialog cancellation
   */
  onDeleteCancelled(): void {
    this.showDeleteDialog = false;
    this.emailConfirmation = '';
    this.deletionCheck = null;
  }

  get firstName() { return this.profileForm.get('firstName'); }
  get lastName() { return this.profileForm.get('lastName'); }
  get newEmail() { return this.emailForm.get('newEmail'); }
}