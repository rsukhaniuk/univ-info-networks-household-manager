import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskService } from '../services/task.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
import { ExecutionService } from '../../executions/services/execution.service';
import { ExecutionHistoryComponent } from '../../executions/execution-history/execution-history.component';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { TaskDetailsDto, TaskPriority, TaskType } from '../../../core/models/task.model';
import { CompleteTaskRequest } from '../../../core/models/execution.model';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-task-details',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, UtcDatePipe, ExecutionHistoryComponent, ConfirmationDialogComponent],
  templateUrl: './task-details.component.html',
  styleUrl: './task-details.component.scss'
})
export class TaskDetailsComponent implements OnInit, OnDestroy {
  @ViewChild(ExecutionHistoryComponent) executionHistory?: ExecutionHistoryComponent;

  private taskService = inject(TaskService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private executionService = inject(ExecutionService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // Data
  householdId: string = '';
  taskId: string = '';
  householdName: string = '';
  householdIsOwner = false;
  taskDetails: TaskDetailsDto | null = null;

  // State
  isLoading = true;
  
  // Complete form
  completeForm: FormGroup;
  isSubmitting = false;
  selectedPhoto: File | null = null;
  photoPreviewUrl: string | null = null;

  // User info
  currentUserId: string = '';
  isSystemAdmin$ = this.authService.isSystemAdmin$();

  // Confirmation dialog state
  showConfirmDialog = false;
  confirmDialogData: ConfirmDialogData = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'danger'
  };
  pendingAction: (() => void) | null = null;

  // Enums
  TaskPriority = TaskPriority;
  TaskType = TaskType;

  constructor() {
    this.completeForm = this.fb.group({
      notes: ['', [Validators.maxLength(1000)]],
      photo: [null]
    });
  }

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('householdId')!;
    this.taskId = this.route.snapshot.paramMap.get('taskId')!;

    // Load household for context
    if (this.householdId) {
      this.loadHousehold();
    }

    this.authService.getUserId$().subscribe(userId => {
      if (userId) {
        this.currentUserId = userId;
      }
    });

    this.loadTaskDetails();
  }

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.householdName = response.data.household.name;
          this.householdIsOwner = response.data.isOwner;

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: response.data.household.id,
            name: response.data.household.name,
            isOwner: response.data.isOwner
          });
        }
      },
      error: (error) => {
        console.error('Failed to load household:', error);
      }
    });
  }

  loadTaskDetails(): void {
    this.isLoading = true;

    this.taskService.getTaskDetails(this.householdId, this.taskId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.taskDetails = response.data;
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.toastService.error(error.message || 'Failed to load task details');
        this.isLoading = false;
      }
    });
  }

  // Complete form methods
  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];

      // Validate file size (5MB max)
      if (file.size > 5 * 1024 * 1024) {
        this.toastService.error('Photo must be less than 5MB');
        input.value = '';
        return;
      }

      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
      if (!allowedTypes.includes(file.type)) {
        this.toastService.error('Only JPEG, PNG, GIF, and WebP images are allowed');
        input.value = '';
        return;
      }

      this.selectedPhoto = file;

      // Generate preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.photoPreviewUrl = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removePhoto(): void {
    this.selectedPhoto = null;
    this.photoPreviewUrl = null;
    this.completeForm.patchValue({ photo: null });
  }

  onCompleteTask(): void {
    if (this.completeForm.invalid || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;

    const request: CompleteTaskRequest = {
      taskId: this.taskId,
      notes: this.completeForm.value.notes || undefined,
      completedAt: new Date()
    };

    this.executionService.completeTask(request, this.selectedPhoto || undefined).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success('Task completed successfully!');
          this.completeForm.reset();
          this.removePhoto();
          this.loadTaskDetails();
          // Reload execution history
          if (this.executionHistory) {
            this.executionHistory.loadExecutions();
          }
        }
        this.isSubmitting = false;
      },
      error: (error) => {
        this.toastService.error(error.message || 'Failed to complete task');
        this.isSubmitting = false;
      }
    });
  }

  // Task actions
  confirmDeleteTask(): void {
    if (!this.taskDetails) return;

    this.showConfirmDialog = true;
    this.confirmDialogData = {
      title: 'Delete Task',
      message: `Are you sure you want to delete "${this.taskDetails.task.title}"?\n\nThis action cannot be undone. All execution history for this task will be permanently deleted.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-trash-alt',
      iconClass: 'text-danger'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;
      this.taskService.deleteTask(this.householdId, this.taskId).subscribe({
        next: () => {
          this.toastService.success(`Task "${this.taskDetails?.task.title}" deleted successfully`);
          setTimeout(() => {
            this.router.navigate(['/tasks', this.householdId]);
          }, 150);
        },
        error: (error) => {
          this.toastService.error(error.message || 'Failed to delete task');
        }
      });
    };
  }

  onConfirmDialogConfirmed(): void {
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }

  onConfirmDialogCancelled(): void {
    this.showConfirmDialog = false;
    this.pendingAction = null;
  }

  activateTask(): void {
    this.taskService.activateTask(this.householdId, this.taskId).subscribe({
      next: () => {
        this.toastService.success('Task activated successfully');
        this.loadTaskDetails();
      },
      error: (error) => {
        this.toastService.error(error.message || 'Failed to activate task');
      }
    });
  }

  deactivateTask(): void {
    this.taskService.deactivateTask(this.householdId, this.taskId).subscribe({
      next: () => {
        this.toastService.success('Task deactivated successfully');
        this.loadTaskDetails();
      },
      error: (error) => {
        this.toastService.error(error.message || 'Failed to deactivate task');
      }
    });
  }

  // Helpers
  getPriorityBadgeClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low:
        return 'bg-success';
      case TaskPriority.Medium:
        return 'bg-warning';
      case TaskPriority.High:
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }

  get canComplete(): boolean {
    if (!this.taskDetails) return false;
    return this.taskDetails.permissions.canComplete;
  }

  get canEdit(): boolean {
    if (!this.taskDetails) return false;
    return this.taskDetails.permissions.canEdit;
  }

  get canDelete(): boolean {
    if (!this.taskDetails) return false;
    return this.taskDetails.permissions.canDelete;
  }

  ngOnDestroy(): void {
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }

  get isOwner(): boolean {
    if (!this.taskDetails) return false;
    return this.taskDetails.permissions.isOwner;
  }
}