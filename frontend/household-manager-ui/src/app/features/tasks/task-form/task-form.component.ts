import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import type { Options } from 'flatpickr/dist/types/options';

import { FlatpickrDirective, FlatpickrDefaultsInterface } from 'angularx-flatpickr';

// Services
import { TaskService } from '../services/task.service';
import { HouseholdService } from '../../households/services/household.service';
import { RoomService } from '../../rooms/services/room.service';

// Models
import { 
  TaskDto, 
  UpsertTaskRequest, 
  TaskPriority, 
  TaskType,
  DayOfWeek,
  TaskDetailsDto 
} from '../../../core/models/task.model';
import { RoomDto } from '../../../core/models/room.model';
import { HouseholdMemberDto } from '../../../core/models/household.model';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    ReactiveFormsModule,
    FlatpickrDirective
  ],
  templateUrl: './task-form.component.html',
  styleUrl: './task-form.component.scss'
})
export class TaskFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private householdService = inject(HouseholdService);
  private roomService = inject(RoomService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // Data
  householdId: string = '';
  taskId: string | null = null;
  isEdit = false;
  
  form: FormGroup;
  rooms: RoomDto[] = [];
  members: HouseholdMemberDto[] = [];

  // State
  isLoading = true;
  isSubmitting = false;
  error: string | null = null;

  // Enums
  TaskPriority = TaskPriority;
  TaskType = TaskType;
  TaskPriorityKeys = Object.keys(TaskPriority).filter(k => isNaN(Number(k)));
  TaskTypeKeys = Object.keys(TaskType).filter(k => isNaN(Number(k)));
  DayOfWeekEnum = DayOfWeek;
  DayOfWeekKeys = Object.keys(DayOfWeek).filter(k => isNaN(Number(k)));

  // ✅ Flatpickr options
  flatpickrOptions: FlatpickrDefaultsInterface = {
    enableTime: false,
    dateFormat: 'Y-m-d',
    minDate: 'today',
    altInput: true,
    altFormat: 'F j, Y',
    allowInput: false
  };

  constructor() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(1000)]],
      roomId: ['', [Validators.required]],
      type: [TaskType.OneTime, [Validators.required]],
      priority: [TaskPriority.Medium, [Validators.required]],
      estimatedMinutes: [30, [Validators.required, Validators.min(5), Validators.max(480)]],
      assignedUserId: ['', [Validators.required]],
      isActive: [true],
      dueDate: [null],
      scheduledWeekday: [null],
      rowVersion: [null]
    });

    // ✅ Watch task type changes
    this.form.get('type')?.valueChanges.subscribe(type => {
      this.onTaskTypeChange(type);
    });
  }

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('householdId')!;
    this.taskId = this.route.snapshot.paramMap.get('taskId');
    this.isEdit = !!this.taskId;

    this.loadFormData();
  }

  loadFormData(): void {
    this.isLoading = true;

    Promise.all([
      this.loadRooms(),
      this.loadMembers(),
      this.isEdit && this.taskId ? this.loadTask() : Promise.resolve()
    ]).then(() => {
      this.isLoading = false;
    }).catch(error => {
      this.error = error.message || 'Failed to load form data';
      this.isLoading = false;
    });
  }

  private loadRooms(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.roomService.getRooms(this.householdId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            // ✅ Handle both array and PagedResult
            this.rooms = Array.isArray(response.data) 
              ? response.data 
              : (response.data as any).items || [];
            resolve();
          } else {
            reject(new Error('Failed to load rooms'));
          }
        },
        error: reject
      });
    });
  }

  private loadMembers(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.householdService.getHouseholdById(this.householdId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.members = response.data.members || [];
            resolve();
          } else {
            reject(new Error('Failed to load members'));
          }
        },
        error: reject
      });
    });
  }

  private loadTask(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.taskService.getTaskDetails(this.householdId, this.taskId!).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            const taskDetails: TaskDetailsDto = response.data;
            const task = taskDetails.task;
            
            this.form.patchValue({
              title: task.title,
              description: task.description,
              roomId: task.roomId,
              type: task.type,
              priority: task.priority,
              estimatedMinutes: task.estimatedMinutes,
              assignedUserId: task.assignedUserId,
              isActive: task.isActive,
              dueDate: task.dueDate ? new Date(task.dueDate).toISOString().split('T')[0] : null,
              scheduledWeekday: task.scheduledWeekday,
              rowVersion: task.rowVersion
            });

            resolve();
          } else {
            reject(new Error('Failed to load task'));
          }
        },
        error: reject
      });
    });
  }

  onTaskTypeChange(type: TaskType): void {
    const dueDateControl = this.form.get('dueDate');
    const scheduledWeekdayControl = this.form.get('scheduledWeekday');

    if (type === TaskType.OneTime) {
      // One-time task: require due date, clear weekday
      dueDateControl?.setValidators([Validators.required]);
      scheduledWeekdayControl?.clearValidators();
      scheduledWeekdayControl?.setValue(null);
    } else {
      // Regular task: require weekday, clear due date
      scheduledWeekdayControl?.setValidators([Validators.required]);
      dueDateControl?.clearValidators();
      dueDateControl?.setValue(null);
    }

    dueDateControl?.updateValueAndValidity();
    scheduledWeekdayControl?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const formValue = this.form.value;
    const request: UpsertTaskRequest = {
      id: this.taskId || undefined,
      householdId: this.householdId,
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      roomId: formValue.roomId,
      type: formValue.type,
      priority: formValue.priority,
      estimatedMinutes: formValue.estimatedMinutes,
      assignedUserId: formValue.assignedUserId || undefined,
      isActive: formValue.isActive,
      dueDate: formValue.dueDate ? new Date(formValue.dueDate) : undefined,
      scheduledWeekday: formValue.scheduledWeekday !== null ? Number(formValue.scheduledWeekday) : undefined,
      rowVersion: formValue.rowVersion
    };

    const operation = this.isEdit
      ? this.taskService.updateTask(this.householdId, this.taskId!, request)
      : this.taskService.createTask(this.householdId, request);

    operation.subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.router.navigate(['/households', this.householdId, 'tasks', response.data.id]);
        }
      },
      error: (error) => {
        this.error = error.message || `Failed to ${this.isEdit ? 'update' : 'create'} task`;
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/households', this.householdId, 'tasks']);
  }

  // Getters for template
  get title() { return this.form.get('title'); }
  get description() { return this.form.get('description'); }
  get roomId() { return this.form.get('roomId'); }
  get type() { return this.form.get('type'); }
  get priority() { return this.form.get('priority'); }
  get estimatedMinutes() { return this.form.get('estimatedMinutes'); }
  get assignedUserId() { return this.form.get('assignedUserId'); }
  get dueDate() { return this.form.get('dueDate'); }
  get scheduledWeekday() { return this.form.get('scheduledWeekday'); }
  get isActive() { return this.form.get('isActive'); }

  // Helper methods
  getPriorityLabel(priority: TaskPriority): string {
    return TaskPriority[priority];
  }

  getTypeLabel(type: TaskType): string {
    return TaskType[type];
  }

  getRoomName(roomId: string): string {
    return this.rooms.find(r => r.id === roomId)?.name || '';
  }

  getMemberName(userId: string): string {
    return this.members.find(m => m.userId === userId)?.userName || '';
  }
}