import { Component, OnInit, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ExecutionService } from '../services/execution.service';
import { ExecutionDto, ExecutionQueryParameters } from '../../../core/models/execution.model';
import { PagedResult } from '../../../core/models/api-response.model';

@Component({
  selector: 'app-execution-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './execution-history.component.html',
  styleUrl: './execution-history.component.scss'
})
export class ExecutionHistoryComponent implements OnInit {
  private executionService = inject(ExecutionService);

  // Inputs for flexibility
  @Input() taskId?: string;           // Show executions for specific task
  @Input() householdId?: string;      // Show executions for household
  @Input() limit?: number;            // Limit results (for task details page)

  // Data
  executions: ExecutionDto[] = [];
  pagedResult: PagedResult<ExecutionDto> | null = null;

  // State
  isLoading = true;
  error: string | null = null;

  // Query params
  queryParams: ExecutionQueryParameters = {
    page: 1,
    pageSize: 20,
    sortBy: 'completedAt',
    sortOrder: 'desc'
  };

  ngOnInit(): void {
    this.loadExecutions();
  }

  loadExecutions(): void {
    this.isLoading = true;
    this.error = null;

    // Determine which endpoint to use
    let observable;

    if (this.taskId) {
      // Load executions for specific task
      observable = this.executionService.getTaskExecutions(this.taskId, this.queryParams);
    } else if (this.householdId) {
      // Load executions for household
      observable = this.executionService.getHouseholdExecutions(this.householdId, this.queryParams);
    } else {
      this.error = 'Either taskId or householdId must be provided';
      this.isLoading = false;
      return;
    }

    observable.subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.pagedResult = response.data;
          
          // Apply limit if specified (for task details page)
          if (this.limit) {
            this.executions = response.data.items.slice(0, this.limit);
          } else {
            this.executions = response.data.items;
          }
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load execution history';
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.queryParams.page = page;
    this.loadExecutions();
  }

  deleteExecution(execution: ExecutionDto): void {
    const confirmed = confirm(`Are you sure you want to delete this execution?\nTask: ${execution.taskTitle}\nCompleted by: ${execution.userName}`);
    if (!confirmed) return;

    this.executionService.deleteExecution(execution.id).subscribe({
      next: () => {
        this.loadExecutions();
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete execution';
      }
    });
  }
}