import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { ToastService } from '../../../core/services/toast.service';

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
  private toastService = inject(ToastService);

  form: FormGroup;
  isSubmitting = false;

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

    this.householdService.joinHousehold({
      inviteCode: this.form.value.inviteCode
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.toastService.success(`Successfully joined "${response.data.name}"!`);
          this.router.navigate(['/households', response.data.id]);
        }
      },
      error: () => {
        this.isSubmitting = false;
        // Errors are handled globally by error interceptor
      }
    });
  }

  get inviteCode() {
    return this.form.get('inviteCode');
  }
}