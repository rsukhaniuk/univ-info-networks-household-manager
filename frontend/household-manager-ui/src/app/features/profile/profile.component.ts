import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from './services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { UserProfileDto, UpdateProfileRequest } from '../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  // Data
  profile: UserProfileDto | null = null;
  auth0User$ = this.authService.user$;

  // State
  isLoading = true;
  isSubmitting = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Form
  profileForm: FormGroup;

  constructor() {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.maxLength(50)]],
      lastName: ['', [Validators.maxLength(50)]]
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.error = null;

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
      error: (error) => {
        this.error = error.message || 'Failed to load profile';
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
    this.error = null;
    this.successMessage = null;

    const request: UpdateProfileRequest = {
      firstName: this.profileForm.value.firstName || undefined,
      lastName: this.profileForm.value.lastName || undefined
    };

    this.userService.updateMyProfile(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = 'Profile updated successfully';
          this.loadProfile(); // Reload to get updated data
          this.autoHideMessage();
        }
        this.isSubmitting = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to update profile';
        this.isSubmitting = false;
      }
    });
  }

  setCurrentHousehold(householdId: string): void {
    this.userService.setCurrentHousehold({ householdId }).subscribe({
      next: () => {
        this.successMessage = 'Current household updated successfully';
        this.loadProfile();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to set current household';
      }
    });
  }

  clearCurrentHousehold(): void {
    this.userService.setCurrentHousehold({ householdId: undefined }).subscribe({
      next: () => {
        this.successMessage = 'Current household cleared';
        this.loadProfile();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to clear current household';
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }

  get firstName() { return this.profileForm.get('firstName'); }
  get lastName() { return this.profileForm.get('lastName'); }
}