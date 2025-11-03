import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  TaskDto,
  TaskDetailsDto,
  UpsertTaskRequest,
  AssignTaskRequest,
  TaskCalendarDto,
  TaskQueryParameters,
  TaskAssignmentPreviewDto
} from '../../../core/models/task.model';
import { ApiResponse, PagedResult } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private api = inject(ApiService);

  /**
   * Get paginated list of tasks for household
   */
  getTasks(
    householdId: string,
    params?: TaskQueryParameters
  ): Observable<ApiResponse<PagedResult<TaskDto>>> {
    return this.api.getPaged<TaskDto>(
      `/households/${householdId}/tasks`,
      params
    );
  }

  /**
   * Get task details with relations
   */
  getTaskDetails(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<TaskDetailsDto>> {
    return this.api.get<TaskDetailsDto>(
      `/households/${householdId}/tasks/${taskId}`
    );
  }

  /**
   * Create new task
   */
  createTask(
    householdId: string,
    request: UpsertTaskRequest
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.post<TaskDto>(
      `/households/${householdId}/tasks`,
      request
    );
  }

  /**
   * Update existing task
   */
  updateTask(
    householdId: string,
    taskId: string,
    request: UpsertTaskRequest
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.put<TaskDto>(
      `/households/${householdId}/tasks/${taskId}`,
      request
    );
  }

  /**
   * Delete task
   */
  deleteTask(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<any>> {
    return this.api.delete(
      `/households/${householdId}/tasks/${taskId}`
    );
  }

  /**
   * Assign task to user
   */
  assignTask(
    householdId: string,
    taskId: string,
    request: AssignTaskRequest
  ): Observable<ApiResponse<TaskDto>> {
    // Backend expects userId as query parameter, not body
    const url = `/households/${householdId}/tasks/${taskId}/assign` +
      (request.userId ? `?userId=${request.userId}` : '');
    return this.api.post<TaskDto>(url, null);
  }

  /**
   * Unassign task
   */
  unassignTask(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.postEmpty<TaskDto>(
      `/households/${householdId}/tasks/${taskId}/unassign`
    );
  }

  /**
   * Preview how tasks would be auto-assigned
   */
  previewAutoAssignTasks(
    householdId: string
  ): Observable<ApiResponse<TaskAssignmentPreviewDto[]>> {
    return this.api.postEmpty<TaskAssignmentPreviewDto[]>(
      `/households/${householdId}/tasks/auto-assign/preview`
    );
  }

  /**
   * Auto-assign all unassigned tasks
   */
  autoAssignTasks(
    householdId: string
  ): Observable<ApiResponse<string>> {
    return this.api.postEmpty<string>(
      `/households/${householdId}/tasks/auto-assign`
    );
  }

  /**
   * Reassign task to next user
   */
  reassignTask(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.postEmpty<TaskDto>(
      `/households/${householdId}/tasks/${taskId}/reassign`
    );
  }

  /**
   * Activate task
   */
  activateTask(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.postEmpty<TaskDto>(
      `/households/${householdId}/tasks/${taskId}/activate`
    );
  }

  /**
   * Deactivate task
   */
  deactivateTask(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<TaskDto>> {
    return this.api.postEmpty<TaskDto>(
      `/households/${householdId}/tasks/${taskId}/deactivate`
    );
  }

  /**
   * Get task calendar for week
   */
  getTaskCalendar(
    householdId: string,
    weekStarting?: Date
  ): Observable<ApiResponse<TaskCalendarDto>> {
    const params = weekStarting ? { weekStarting: weekStarting.toISOString() } : {};
    return this.api.get<TaskCalendarDto>(
      `/households/${householdId}/tasks/calendar`,
      params
    );
  }

  /**
   * Invalidate execution for this week (allows task to be recompleted)
   */
  invalidateExecutionThisWeek(
    householdId: string,
    taskId: string
  ): Observable<ApiResponse<string>> {
    return this.api.postEmpty<string>(
      `/households/${householdId}/tasks/${taskId}/invalidate-execution`
    );
  }
}