import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';

import { FlatpickrDirective, FlatpickrDefaults } from 'angularx-flatpickr';
import { english } from 'flatpickr/dist/l10n/default';

// Services
import { TaskService } from '../services/task.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
import { RoomService } from '../../rooms/services/room.service';
import { ToastService } from '../../../core/services/toast.service';

// Models
import {
  UpsertTaskRequest,
  TaskPriority,
  TaskType,
  TaskDetailsDto
} from '../../../core/models/task.model';
import { RoomDto } from '../../../core/models/room.model';
import { HouseholdMemberDto } from '../../../core/models/household.model';
import { RecurrenceRuleBuilderComponent } from '../../../shared/components/recurrence-rule-builder/recurrence-rule-builder.component';

@Component({
  selector: 'app-task-form',
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    FormsModule,
    FlatpickrDirective,
    RecurrenceRuleBuilderComponent
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
  private location = inject(Location);
  private toastService = inject(ToastService);

  // Data
  householdId: string = '';
  taskId: string | null = null;
  isEdit = false;
  householdName: string = '';
  isOwner = false;

  form: FormGroup;
  rooms: RoomDto[] = [];
  members: HouseholdMemberDto[] = [];
  generalRoom: RoomDto | null = null;
  isGeneralTask = false;

  // State
  isLoading = true;
  isSubmitting = false;

  // Enums
  TaskPriority = TaskPriority;
  TaskType = TaskType;
  TaskPriorityKeys = Object.keys(TaskPriority).filter(k => isNaN(Number(k)));
  TaskTypeKeys = Object.keys(TaskType).filter(k => isNaN(Number(k)));

  // Helper: parse backend UTC string into a local Date
  // Some API responses may omit the trailing 'Z'. In that case
  // JS parses it as local time (no TZ shift). To keep behavior
  // consistent with the details view pipe (utcDate), we normalize
  // strings to UTC by appending 'Z' when no TZ info is present.
  private parseUtcToLocal(value: string | Date | null | undefined): Date | null {
    if (!value) return null;
    if (value instanceof Date) return value;

    const hasTz = /Z$|[+-]\d{2}:?\d{2}$/.test(value);
    const normalized = hasTz ? value : value + 'Z';
    const d = new Date(normalized);
    return isNaN(d.getTime()) ? null : d;
  }

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

  // Flatpickr options with date AND time (strict 24-hour format, no past times)
  flatpickrOptions: any = {
    enableTime: true,
    noCalendar: false,
    time24hr: true,
    dateFormat: 'Y-m-d H:i',
    minDate: new Date(),
    altInput: true,
    altFormat: 'j F Y, H:i',
    allowInput: false,
    minuteIncrement: 1,
    locale: {
      ...english,
      firstDayOfWeek: 1,
      // Force 24-hour format in Ukrainian locale
      time24hr: true
    },

    // Update minDate every time picker opens to prevent selecting past times
    onOpen: (_selectedDates: Date[], _dateStr: string, instance: any) => {
      instance.set('minDate', new Date());
      instance.set('time24hr', true);
    },

    onReady: (_sel: Date[], _str: string, inst: any) => {
      inst.set('time24hr', true);
      // Force 24-hour time format
      inst.hourElement.setAttribute('max', '23');
      inst.hourElement.step = '1';
    },

    // Validate that selected time is not in the past
    onChange: (selectedDates: Date[], _dateStr: string, instance: any) => {
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
      recurrenceRule: [null],
      rowVersion: [null]
    });

    // Watch task type changes
    this.form.get('type')?.valueChanges.subscribe(type => {
      this.onTaskTypeChange(type);
    });

    // Watch roomId changes to sync isGeneralTask checkbox
    this.form.get('roomId')?.valueChanges.subscribe(roomId => {
      if (this.generalRoom) {
        this.isGeneralTask = roomId === this.generalRoom.id;
      }
    });
  }

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('householdId')!;
    this.taskId = this.route.snapshot.paramMap.get('taskId');
    this.isEdit = !!this.taskId;

    // Validate householdId
    if (!this.householdId) {
      this.toastService.error('Household ID is missing from the route. Please navigate from a household page.');
      this.isLoading = false;
      console.error('Missing householdId in route params:', this.route.snapshot.paramMap);
      return;
    }

    console.log('Task form initialized with householdId:', this.householdId);

    // Check if roomId is provided in query params (coming from room details)
    const preselectedRoomId = this.route.snapshot.queryParamMap.get('roomId');
    if (preselectedRoomId && !this.isEdit) {
      this.form.patchValue({ roomId: preselectedRoomId });
    }

    this.loadFormData();
  }

  loadFormData(): void {
    this.isLoading = true;

    Promise.all([
      this.loadRooms(),
      this.loadMembers(),
      this.isEdit && this.taskId ? this.loadTask() : Promise.resolve()
    ]).then(() => {
      // Check if there are any rooms
      if (this.rooms.length === 0 && !this.isEdit) {
        this.toastService.error('You need to create at least one room before creating tasks.');
        this.location.back();
        return;
      }

      this.isLoading = false;
    }).catch(() => {
      // Error will be shown in global error banner by error interceptor
      this.isLoading = false;
    });
  }

  private loadRooms(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.roomService.getRooms(this.householdId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            // Handle both array and PagedResult
            this.rooms = Array.isArray(response.data)
              ? response.data
              : (response.data as any).items || [];

            // Find the "General" room
            this.generalRoom = this.rooms.find(r => r.name === 'General') || null;

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
            
            // Convert UTC dueDate to local time for display in flatpickr
            const localDueDate = this.parseUtcToLocal(task.dueDate as any);

            this.form.patchValue({
              title: task.title,
              description: task.description,
              roomId: task.roomId,
              type: task.type,
              priority: task.priority,
              estimatedMinutes: task.estimatedMinutes,
              assignedUserId: task.assignedUserId,
              isActive: task.isActive,
              dueDate: localDueDate,
              recurrenceRule: task.recurrenceRule,
              rowVersion: task.rowVersion
            });

            // Check if this is a General task
            if (this.generalRoom && task.roomId === this.generalRoom.id) {
              this.isGeneralTask = true;
            }

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
    const recurrenceRuleControl = this.form.get('recurrenceRule');

    if (type === TaskType.OneTime || type === 1) {
      // One-time task: require due date with future date validation
      dueDateControl?.setValidators([Validators.required, this.futureDateValidator()]);
      recurrenceRuleControl?.clearValidators();
      recurrenceRuleControl?.setValue(null);
    } else {
      // Regular task: require recurrence rule, clear due date
      recurrenceRuleControl?.setValidators([Validators.required]);
      dueDateControl?.clearValidators();
      dueDateControl?.setValue(null);
    }

    dueDateControl?.updateValueAndValidity();
    recurrenceRuleControl?.updateValueAndValidity();
  }

  onGeneralTaskChange(isGeneral: boolean): void {
    this.isGeneralTask = isGeneral;

    if (isGeneral && this.generalRoom) {
      // Auto-select General room
      this.form.patchValue({ roomId: this.generalRoom.id });
    } else if (!isGeneral) {
      // Clear room selection when unchecking
      this.form.patchValue({ roomId: '' });
    }
  }

  onSubmit(): void {
    // ✅ Revalidate due date before submit (in case time has passed)
    if (this.form.get('type')?.value === TaskType.OneTime) {
      this.form.get('dueDate')?.updateValueAndValidity();
    }

    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    // ✅ Validate householdId
    if (!this.householdId) {
      return;
    }

    this.isSubmitting = true;

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
      recurrenceRule: formValue.recurrenceRule || undefined,
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
          // Show success toast
          const action = this.isEdit ? 'updated' : 'created';
          this.toastService.success(`Task ${action} successfully`);

          // Navigate back to task list instead of task details
          this.location.back();
        }
      },
      error: () => {
        // Error will be shown in global error banner by error interceptor
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
  get recurrenceRule() { return this.form.get('recurrenceRule'); }
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
  }

  getRoomName(roomId: string): string {
    return this.rooms.find(r => r.id === roomId)?.name || '';
  }

  getMemberName(userId: string): string {
    const member = this.members.find(m => m.userId === userId);
    return member?.userName || member?.email || '';
  }
}
