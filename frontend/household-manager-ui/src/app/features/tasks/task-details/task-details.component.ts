import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskService } from '../services/task.service';
import { ExecutionService } from '../../executions/services/execution.service';
import { AuthService } from '../../../core/services/auth.service';
import { TaskDetailsDto, TaskPriority, TaskType } from '../../../core/models/task.model';
import { ExecutionDto, CompleteTaskRequest } from '../../../core/models/execution.model';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-task-details',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, UtcDatePipe],
  templateUrl: './task-details.component.html',
  styleUrl: './task-details.component.scss'
})
export class TaskDetailsComponent implements OnInit {
  private taskService = inject(TaskService);
  private executionService = inject(ExecutionService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // Data
  householdId: string = '';
  taskId: string = '';
  taskDetails: TaskDetailsDto | null = null;
  recentExecutions: ExecutionDto[] = [];

  // State
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;
  
  // Complete form
  completeForm: FormGroup;
  isSubmitting = false;
  selectedPhoto: File | null = null;
  photoPreviewUrl: string | null = null;

  // User info
  currentUserId: string = '';
  isSystemAdmin$ = this.authService.isSystemAdmin$();

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

    this.authService.getUserId$().subscribe(userId => {
      if (userId) {
        this.currentUserId = userId;
      }
    });

    this.loadTaskDetails();
  }

  loadTaskDetails(): void {
    this.isLoading = true;
    this.error = null;

    this.taskService.getTaskDetails(this.householdId, this.taskId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.taskDetails = response.data;
          this.recentExecutions = response.data.recentExecutions.slice(0, 5);
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load task details';
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
        this.error = 'Photo must be less than 5MB';
        input.value = '';
        return;
      }

      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
      if (!allowedTypes.includes(file.type)) {
        this.error = 'Only JPEG, PNG, GIF, and WebP images are allowed';
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
    this.error = null;

    const request: CompleteTaskRequest = {
      taskId: this.taskId,
      notes: this.completeForm.value.notes || undefined,
      completedAt: new Date()
    };

    this.executionService.completeTask(request, this.selectedPhoto || undefined).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = 'Task completed successfully!';
          this.completeForm.reset();
          this.removePhoto();
          this.loadTaskDetails();
          this.autoHideMessage();
        }
        this.isSubmitting = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to complete task';
        this.isSubmitting = false;
      }
    });
  }

  // Task actions
  deleteTask(): void {
    if (!this.taskDetails) return;

    const confirmed = confirm(`Are you sure you want to delete "${this.taskDetails.task.title}"? This action cannot be undone.`);
    if (!confirmed) return;

    this.taskService.deleteTask(this.householdId, this.taskId).subscribe({
      next: () => {
        this.router.navigate(['/households', this.householdId, 'tasks']);
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete task';
      }
    });
  }

  activateTask(): void {
    this.taskService.activateTask(this.householdId, this.taskId).subscribe({
      next: () => {
        this.successMessage = 'Task activated successfully';
        this.loadTaskDetails();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to activate task';
      }
    });
  }

  deactivateTask(): void {
    this.taskService.deactivateTask(this.householdId, this.taskId).subscribe({
      next: () => {
        this.successMessage = 'Task deactivated successfully';
        this.loadTaskDetails();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to deactivate task';
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

  get isOwner(): boolean {
    if (!this.taskDetails) return false;
    return this.taskDetails.permissions.isOwner;
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }
}