import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TableLazyLoadEvent } from 'primeng/table';
import { Subject, debounceTime, finalize } from 'rxjs';

import { TaskService } from '../services/task.service';
import { CalendarService, CalendarSubscriptionDto } from '../services/calendar.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingService } from '../../../core/services/loading.service';
import { ToastService } from '../../../core/services/toast.service';
import { TaskDto, TaskPriority, TaskType, TaskAssignmentPreviewDto } from '../../../core/models/task.model';
import { RecurrenceRuleService } from '../../../shared/services/recurrence-rule.service';
import { HouseholdDto } from '../../../core/models/household.model';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { AutoAssignPreviewDialogComponent } from '../../../shared/components/auto-assign-preview-dialog/auto-assign-preview-dialog.component';
import { CalendarSubscriptionDialogComponent } from '../../../shared/components/calendar-subscription-dialog/calendar-subscription-dialog.component';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

type SortBy = 'title' | 'priority' | 'createdAt' | 'dueDate' | 'roomName' | 'type' | 'isActive' | 'assignedUserName';
type SortOrder = 'asc' | 'desc';

@Component({
  selector: 'app-task-list',
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TableModule,
    InputTextModule,
    ButtonModule,
    SkeletonModule,
    ConfirmationDialogComponent,
    AutoAssignPreviewDialogComponent,
    CalendarSubscriptionDialogComponent,
    UtcDatePipe
  ],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss'
})
export class TaskListComponent implements OnInit, OnDestroy {
  private taskService = inject(TaskService);
  private calendarService = inject(CalendarService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  private loadingService = inject(LoadingService);
  private toastService = inject(ToastService);
  private recurrenceRuleService = inject(RecurrenceRuleService);

  isSystemAdmin$ = this.auth.isSystemAdmin$();

  householdId: string = '';
  household: HouseholdDto | null = null;
  isOwner = false;
  canManageTasks = false;

  loading = true;
  private loadingDelayMs = 400;
  private loadingTimer: any = null;
  rows = 10;
  first = 0;
  totalRecords = 0;

  items: TaskDto[] = [];

  searchQuery = '';
  private search$ = new Subject<string>();
  private searchSubscription: any;

  currentSortField: SortBy = 'priority';
  currentSortOrder: SortOrder = 'desc';

  // Filters (using empty string for select compatibility)
  filterRoomId: string = '';
  filterType: string = '';
  filterPriority: string = '';
  filterAssignedUserId: string = '';
  filterIsActive: string = '';
  filterIsOverdue: string = '';
  filterMyTasksOnly: boolean = false;
  currentUserId: string = '';

  // Data for filter dropdowns
  availableRooms: { id: string; name: string }[] = [];
  availableAssignees: { userId: string; userName: string }[] = [];

  showConfirmDialog = false;
  confirmDialogData: ConfirmDialogData = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'danger'
  };
  private pendingAction: (() => void) | null = null;

  // Auto-assign preview
  showAutoAssignPreview = false;
  autoAssignPreview: TaskAssignmentPreviewDto[] = [];

  // Calendar subscription
  showCalendarSubscriptionDialog = false;
  calendarSubscriptionData: CalendarSubscriptionDto | null = null;

  // Enums for template
  TaskPriority = TaskPriority;
  TaskType = TaskType;

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('householdId')!;

    // Get current user ID for "My Tasks" filter
    this.auth.getUserId$().subscribe(userId => {
      if (userId) {
        this.currentUserId = userId;
      }
    });

    if (this.householdId) {
      this.loadHousehold();
    }

    this.searchSubscription = this.search$
      .pipe(debounceTime(300))
      .subscribe(q => {
        this.searchQuery = q;
        this.first = 0;
        this.loadData();
      });
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
    }
  }

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data.household;
          this.isOwner = response.data.isOwner;
          this.updateCanManageTasks();

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: this.household.id,
            name: this.household.name,
            isOwner: this.isOwner
          });

          // Load filter options from details response
          this.loadFilterOptions(response.data);
        }
      },
      error: (error) => {
        // Error will be shown by error interceptor
      }
    });
  }

  private loadFilterOptions(details: any): void {
    // Load rooms for filter
    if (details?.rooms) {
      this.availableRooms = details.rooms.map((r: any) => ({
        id: r.id,
        name: r.name
      }));
    }

    // Load members for filter
    if (details?.members) {
      this.availableAssignees = details.members.map((m: any) => ({
        userId: m.userId,
        userName: m.userName
      }));
    }
  }

  private updateCanManageTasks(): void {
    this.auth.isSystemAdmin$().subscribe(isAdmin => {
      this.canManageTasks = this.isOwner || isAdmin;
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.rows;
    const page = Math.floor((event.first ?? 0) / pageSize) + 1;

    const sortField = (event.sortField as SortBy) ?? 'priority';
    const sortOrder: SortOrder = (event.sortOrder ?? -1) === 1 ? 'asc' : 'desc';

    this.currentSortField = sortField;
    this.currentSortOrder = sortOrder;

    this.rows = pageSize;
    this.first = (page - 1) * pageSize;

    this.loadData(page, pageSize, sortField, sortOrder);
  }

  private loadData(
    page: number = 1,
    pageSize: number = this.rows,
    sortBy: SortBy = this.currentSortField,
    sortOrder: SortOrder = this.currentSortOrder
  ): void {
    if (this.showConfirmDialog) {
      return;
    }

    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loadingTimer = setTimeout(() => {
      this.loading = true;
      this.loadingTimer = null;
    }, this.loadingDelayMs);

    this.taskService
      .getTasks(this.householdId, {
        page,
        pageSize,
        sortBy,
        sortOrder,
        search: this.searchQuery || undefined,
        roomId: this.filterRoomId || undefined,
        type: this.filterType ? +this.filterType as TaskType : undefined,
        priority: this.filterPriority ? +this.filterPriority as TaskPriority : undefined,
        assignedUserId: this.filterAssignedUserId || undefined,
        isActive: this.filterIsActive === 'true' ? true : this.filterIsActive === 'false' ? false : undefined,
        isOverdue: this.filterIsOverdue === 'true' ? true : this.filterIsOverdue === 'false' ? false : undefined
      })
      .pipe(
        finalize(() => {
          if (this.loadingTimer) {
            clearTimeout(this.loadingTimer);
            this.loadingTimer = null;
          }
          this.loading = false;
        })
      )
      .subscribe({
        next: (res) => {
          this.items = res.data?.items ?? [];
          this.totalRecords = res.data?.totalCount ?? 0;
        },
        error: (err) => {
          // Error will be shown by error interceptor
          this.items = [];
          this.totalRecords = 0;
        }
      });
  }

  onSearch(query: string): void {
    this.search$.next(query);
  }

  onPageSizeChange(size: number): void {
    this.rows = size;
    this.first = 0;
    this.loadData(1, size);
  }

  reload(): void {
    const currentPage = Math.floor(this.first / this.rows) + 1;
    this.loadData(currentPage, this.rows);
  }

  confirmDelete(task: TaskDto): void {
    this.loadingService.beginSuppress();

    this.showConfirmDialog = true;

    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loading = false;

    this.confirmDialogData = {
      title: 'Delete Task',
      message: `Are you sure you want to delete "${task.title}"?\n\nThis action cannot be undone. All execution history for this task will be permanently deleted.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-trash-alt',
      iconClass: 'text-danger'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;

      this.loadingService.endSuppress();

      this.taskService.deleteTask(this.householdId, task.id).subscribe({
        next: () => {
          this.toastService.success(`Task "${task.title}" deleted successfully`);
          setTimeout(() => this.reload(), 150);
        },
        error: (err) => {
          // Error will be shown by error interceptor
        }
      });
    };
  }

  onDialogConfirmed(): void {
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }

  onDialogCancelled(): void {
    this.pendingAction = null;
    this.showConfirmDialog = false;

    this.loadingService.endSuppress();
  }

  onFilterChange(): void {
    this.first = 0;
    this.loadData();
  }

  onMyTasksToggle(): void {
    if (this.filterMyTasksOnly) {
      this.filterAssignedUserId = this.currentUserId;
    } else {
      this.filterAssignedUserId = '';
    }
    this.first = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterRoomId = '';
    this.filterType = '';
    this.filterPriority = '';
    this.filterAssignedUserId = '';
    this.filterIsActive = '';
    this.filterIsOverdue = '';
    this.filterMyTasksOnly = false;
    this.first = 0;
    this.loadData();
  }

  hasActiveFilters(): boolean {
    return !!(
      this.searchQuery ||
      this.filterRoomId ||
      this.filterType ||
      this.filterPriority ||
      this.filterAssignedUserId ||
      this.filterIsActive ||
      this.filterIsOverdue ||
      this.filterMyTasksOnly
    );
  }

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

  getTypeBadgeClass(type: TaskType): string {
    return type === TaskType.OneTime ? 'bg-info' : 'bg-primary';
  }

  getStatusBadgeClass(isActive: boolean): string {
    return isActive ? 'bg-success' : 'bg-secondary';
  }

  formatRecurrenceRule(rrule: string | null | undefined): string {
    return this.recurrenceRuleService.formatRule(rrule);
  }

  // Calendar export methods
  exportCalendar(): void {
    this.calendarService.exportCalendar(this.householdId)
      .then(() => {
        this.toastService.success('Calendar exported successfully. Check your downloads.');
      })
      .catch(() => {
        // Error will be shown by error interceptor (which now handles blob errors)
      });
  }

  openSubscriptionDialog(): void {
    this.calendarService.getSubscriptionUrl(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.calendarSubscriptionData = response.data;
          this.showCalendarSubscriptionDialog = true;
        } else {
          this.toastService.error('Failed to get subscription URL');
        }
      },
      error: (error) => {
        this.toastService.error('Failed to get subscription URL');
        console.error('Subscription URL error:', error);
      }
    });
  }

  closeSubscriptionDialog(): void {
    this.showCalendarSubscriptionDialog = false;
    this.calendarSubscriptionData = null;
  }

  autoAssignTasks(): void {
    this.loadingService.beginSuppress();

    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loading = false;

    // Get preview first
    this.taskService.previewAutoAssignTasks(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.autoAssignPreview = response.data;
          this.showAutoAssignPreview = true;
        }
        this.loadingService.endSuppress();
      },
      error: (err) => {
        // Error will be shown by error interceptor
        this.loadingService.endSuppress();
      }
    });
  }

  onAutoAssignPreviewConfirmed(): void {
    this.showAutoAssignPreview = false;

    // Apply the assignments
    this.taskService.autoAssignTasks(this.householdId).subscribe({
      next: (response) => {
        this.toastService.success(response.message || 'Tasks auto-assigned successfully');
        setTimeout(() => this.reload(), 150);
      },
      error: (err) => {
        // Error will be shown by error interceptor
      }
    });
  }

  onAutoAssignPreviewCancelled(): void {
    this.showAutoAssignPreview = false;
  }
}
