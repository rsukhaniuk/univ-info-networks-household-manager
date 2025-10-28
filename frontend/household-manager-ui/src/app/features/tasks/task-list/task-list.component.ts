import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../services/task.service';
import { AuthService } from '../../../core/services/auth.service';
import { TaskDto, TaskPriority, TaskType, TaskQueryParameters } from '../../../core/models/task.model';
import { PagedResult } from '../../../core/models/api-response.model';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss'
})
export class TaskListComponent implements OnInit {
  private taskService = inject(TaskService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);

  // Data
  householdId: string = '';
  householdName: string = '';
  tasks: TaskDto[] = [];
  pagedResult: PagedResult<TaskDto> | null = null;

  // State
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Filters
  queryParams: TaskQueryParameters = {
    page: 1,
    pageSize: 25,
    sortBy: 'priority',
    sortOrder: 'desc'
  };

  // User info
  currentUserId: string = '';
  isSystemAdmin$ = this.authService.isSystemAdmin$();

  // Enums for template
  TaskPriority = TaskPriority;
  TaskType = TaskType;

  ngOnInit(): void {
    this.householdId = this.route.snapshot.paramMap.get('householdId')!;
    
    this.authService.getUserId$().subscribe(userId => {
      if (userId) {
        this.currentUserId = userId;
      }
    });

    // Get query params from URL
    this.route.queryParams.subscribe(params => {
      this.queryParams = {
        ...this.queryParams,
        search: params['search'],
        roomId: params['roomId'],
        priority: params['priority'] ? +params['priority'] : undefined,
        assignedUserId: params['assignedUserId'],
        isActive: params['status'] === 'active' ? true : params['status'] === 'inactive' ? false : undefined
      };
      
      this.loadTasks();
    });
  }

  loadTasks(): void {
    this.isLoading = true;
    this.error = null;

    this.taskService.getTasks(this.householdId, this.queryParams).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.pagedResult = response.data;
          this.tasks = response.data.items;
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load tasks';
        this.isLoading = false;
      }
    });
  }

  onSearch(searchTerm: string): void {
    this.queryParams.search = searchTerm;
    this.queryParams.page = 1;
    this.loadTasks();
  }

  onFilterChange(filterName: string, value: any): void {
    (this.queryParams as any)[filterName] = value;
    this.queryParams.page = 1;
    this.loadTasks();
  }

  onPageChange(page: number): void {
    this.queryParams.page = page;
    this.loadTasks();
  }

  onSort(sortBy: string): void {
    if (this.queryParams.sortBy === sortBy) {
      this.queryParams.sortOrder = this.queryParams.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.queryParams.sortBy = sortBy;
      this.queryParams.sortOrder = 'desc';
    }
    this.loadTasks();
  }

  clearFilters(): void {
    this.queryParams = {
      page: 1,
      pageSize: 25,
      sortBy: 'priority',
      sortOrder: 'desc'
    };
    this.loadTasks();
  }

  deleteTask(taskId: string, taskTitle: string): void {
    if (!confirm(`Are you sure you want to delete "${taskTitle}"? This action cannot be undone.`)) {
      return;
    }

    this.taskService.deleteTask(this.householdId, taskId).subscribe({
      next: () => {
        this.successMessage = `Task "${taskTitle}" deleted successfully`;
        this.loadTasks();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete task';
      }
    });
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

  isAssignedToCurrentUser(task: TaskDto): boolean {
    return task.assignedUserId === this.currentUserId;
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }
}