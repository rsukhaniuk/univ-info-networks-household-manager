import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HouseholdService } from '../services/household.service';

@Component({
  selector: 'app-join-household',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './join-household.component.html',
  styleUrls: ['./join-household.component.scss']
})

export class JoinHouseholdComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private householdService = inject(HouseholdService);

  form: FormGroup;
  isSubmitting = false;
  error: string | null = null;

  constructor() {
    this.form = this.fb.group({
      inviteCode: ['', [Validators.required]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    this.householdService.joinHousehold({
      inviteCode: this.form.value.inviteCode
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.router.navigate(['/households', response.data.id]);
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to join household. Please check the invite code.';
        this.isSubmitting = false;
      }
    });
  }

  get inviteCode() {
    return this.form.get('inviteCode');
  }
}