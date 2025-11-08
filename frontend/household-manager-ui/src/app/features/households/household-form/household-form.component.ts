import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { HouseholdContext } from '../services/household-context';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-household-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './household-form.component.html',
  styleUrl: './household-form.component.scss'
})
export class HouseholdFormComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private toastService = inject(ToastService);
  private location = inject(Location);

  form!: FormGroup;
  isEditMode = false;
  householdId: string | null = null;
  householdName: string | null = null;
  isSubmitting = false;

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
          this.householdName = response.data.household.name;
          this.form.patchValue({
            name: response.data.household.name,
            description: response.data.household.description
          });

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: response.data.household.id,
            name: response.data.household.name,
            isOwner: response.data.isOwner
          });
        }
      },
      error: (error) => {
        // Error will be shown in global error banner by error interceptor
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

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
          const message = this.isEditMode 
            ? 'Household updated successfully' 
            : 'Household created successfully';
          this.toastService.success(message);
          this.router.navigate(['/households', response.data.id]);
        }
      },
      error: () => {
        this.isSubmitting = false;
        // Errors are handled globally by error interceptor
      }
    });
  }

  onCancel(): void {
    this.location.back();
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

  ngOnDestroy(): void {
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }
}