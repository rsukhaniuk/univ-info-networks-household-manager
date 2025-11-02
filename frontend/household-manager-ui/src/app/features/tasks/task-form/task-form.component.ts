import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';

import { FlatpickrDirective, FlatpickrDefaults } from 'angularx-flatpickr';

// Services
import { TaskService } from '../services/task.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
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
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FlatpickrDirective
  ],
  providers: [FlatpickrDefaults],
  templateUrl: './task-form.component.html',
  styleUrl: './task-form.component.scss'
})
export class TaskFormComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private roomService = inject(RoomService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);

  // Data
  householdId: string = '';
  taskId: string | null = null;
  isEdit = false;
  householdName: string = '';
  isOwner = false;

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

  // ✅ Custom validator for future date/time
  futureDateValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }
      const selectedDate = new Date(control.value);
      const now = new Date();
      
      if (selectedDate <= now) {
        return { pastDate: true };
      }
      return null;
    };
  }

  // ✅ Flatpickr options with date AND time (strict 24-hour format, no past times)
  flatpickrOptions: any = {
    enableTime: true,
    noCalendar: false,
    dateFormat: 'Y-m-d H:i',
    time_24hr: true,
    minDate: new Date(),
    altInput: true,
    altFormat: 'j F Y, H:i',
    allowInput: false,
    minuteIncrement: 1,
    defaultHour: 12,
    defaultMinute: 0,
    locale: {
      firstDayOfWeek: 1
    },
    // Update minDate every time picker opens to prevent selecting past times
    onOpen: (selectedDates: Date[], dateStr: string, instance: any) => {
      const now = new Date();
      instance.set('minDate', now);
      instance.set('time_24hr', true);
    },
    // Validate that selected time is not in the past
    onChange: (selectedDates: Date[], dateStr: string, instance: any) => {
      if (selectedDates.length > 0) {
        const selected = selectedDates[0];
        const now = new Date();

        // If selected date/time is in the past, reset to current time
        if (selected < now) {
          instance.setDate(now, false);
        }
      }
    }
  };

  constructor() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(1000)]],
      roomId: ['', [Validators.required]],
      type: [TaskType.OneTime, [Validators.required]],
      priority: [TaskPriority.Medium, [Validators.required]],
      estimatedMinutes: [30],
      assignedUserId: [''],
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

    // ✅ Validate householdId
    if (!this.householdId) {
      this.error = 'Household ID is missing from the route. Please navigate from a household page.';
      this.isLoading = false;
      console.error('Missing householdId in route params:', this.route.snapshot.paramMap);
      return;
    }

    console.log('Task form initialized with householdId:', this.householdId);

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
            this.householdName = response.data.household.name;
            this.isOwner = response.data.isOwner;

            // Set household context for navigation
            this.householdContext.setHousehold({
              id: response.data.household.id,
              name: response.data.household.name,
              isOwner: response.data.isOwner
            });

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

  onTaskTypeChange(type: any): void {
    const dueDateControl = this.form.get('dueDate');
    const scheduledWeekdayControl = this.form.get('scheduledWeekday');

    if (type === TaskType.OneTime || type === 1) {
      // One-time task: require due date with future date validation
      dueDateControl?.setValidators([Validators.required, this.futureDateValidator()]);
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
    // ✅ Revalidate due date before submit (in case time has passed)
    if (this.form.get('type')?.value === TaskType.OneTime) {
      this.form.get('dueDate')?.updateValueAndValidity();
    }

    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      
      // Show specific error message for past date
      if (this.form.get('dueDate')?.errors?.['pastDate']) {
        this.error = 'Due date must be in the future. Please select a later time.';
      }
      return;
    }

    // ✅ Validate householdId
    if (!this.householdId) {
      this.error = 'Household ID is required. Please navigate from a household.';
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const formValue = this.form.value;
    
    // ✅ Prepare dueDate in ISO format for API
    let dueDateForApi: string | undefined = undefined;
    if (formValue.dueDate) {
      const date = new Date(formValue.dueDate);
      dueDateForApi = date.toISOString();
    }

    const request: UpsertTaskRequest = {
      id: this.taskId || undefined,
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      roomId: formValue.roomId,
      type: formValue.type,
      priority: formValue.priority,
      assignedUserId: formValue.assignedUserId || undefined,
      isActive: formValue.isActive,
      dueDate: dueDateForApi as any,
      scheduledWeekday: formValue.scheduledWeekday !== null ? Number(formValue.scheduledWeekday) : undefined,
      rowVersion: formValue.rowVersion
    };

    console.log('Creating task with request:', JSON.stringify(request, null, 2));
    console.log('HouseholdId (in URL):', this.householdId);

    const operation = this.isEdit
      ? this.taskService.updateTask(this.householdId, this.taskId!, request)
      : this.taskService.createTask(this.householdId, request);

    operation.subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.router.navigate(['/tasks', this.householdId, response.data.id]);
        }
      },
      error: (error) => {
        this.error = error.message || `Failed to ${this.isEdit ? 'update' : 'create'} task`;
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.location.back();
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

  ngOnDestroy(): void {
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }

  getRoomName(roomId: string): string {
    return this.rooms.find(r => r.id === roomId)?.name || '';
  }

  getMemberName(userId: string): string {
    const member = this.members.find(m => m.userId === userId);
    return member?.userName || member?.email || '';
  }
}