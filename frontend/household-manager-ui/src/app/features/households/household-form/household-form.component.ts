import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HouseholdService } from '../services/household.service';

@Component({
  selector: 'app-household-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './household-form.component.html',
  styleUrl: './household-form.component.scss'
})
export class HouseholdFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private householdService = inject(HouseholdService);

  form!: FormGroup;
  isEditMode = false;
  householdId: string | null = null;
  isSubmitting = false;
  error: string | null = null;

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.householdId;

    this.initForm();

    if (this.isEditMode && this.householdId) {
      this.loadHousehold(this.householdId);
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  private loadHousehold(id: string): void {
    this.householdService.getHouseholdById(id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.form.patchValue({
            name: response.data.household.name,
            description: response.data.household.description
          });
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to load household';
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const request = {
      name: this.form.value.name,
      description: this.form.value.description || undefined
    };

    const operation = this.isEditMode && this.householdId
      ? this.householdService.updateHousehold(this.householdId, request)
      : this.householdService.createHousehold(request);

    operation.subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.router.navigate(['/households', response.data.id]);
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to save household';
        this.isSubmitting = false;
      }
    });
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Household' : 'Create Household';
  }

  get submitButtonText(): string {
    return this.isEditMode ? 'Update Household' : 'Create Household';
  }

  get submitButtonClass(): string {
    return this.isEditMode ? 'btn-warning' : 'btn-primary';
  }

  // Form field getters
  get name() {
    return this.form.get('name');
  }

  get description() {
    return this.form.get('description');
  }
}