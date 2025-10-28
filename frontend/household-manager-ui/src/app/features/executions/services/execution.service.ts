import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  ExecutionDto,
  CompleteTaskRequest,
  UpdateExecutionRequest,
  ExecutionQueryParameters
} from '../../../core/models/execution.model';
import { ApiResponse, PagedResult } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class ExecutionService {
  private api = inject(ApiService);

  /**
   * Get execution by ID
   */
  getExecution(id: string): Observable<ApiResponse<ExecutionDto>> {
    return this.api.get<ExecutionDto>(`/executions/${id}`);
  }

  /**
   * Get executions for task
   */
  getTaskExecutions(
    taskId: string,
    params?: ExecutionQueryParameters
  ): Observable<ApiResponse<PagedResult<ExecutionDto>>> {
    return this.api.getPaged<ExecutionDto>(
      `/executions/task/${taskId}`,
      params
    );
  }

  /**
   * Get executions for household
   */
  getHouseholdExecutions(
    householdId: string,
    params?: ExecutionQueryParameters
  ): Observable<ApiResponse<PagedResult<ExecutionDto>>> {
    return this.api.getPaged<ExecutionDto>(
      `/executions/household/${householdId}`,
      params
    );
  }

  /**
   * Get user's executions this week
   */
  getMyExecutionsThisWeek(
    householdId: string
  ): Observable<ApiResponse<ExecutionDto[]>> {
    return this.api.get<ExecutionDto[]>(
      `/executions/household/${householdId}/my-week`
    );
  }

  /**
   * Get weekly executions for household
   */
  getWeeklyExecutions(
    householdId: string,
    weekStarting?: Date
  ): Observable<ApiResponse<ExecutionDto[]>> {
    const params = weekStarting ? { weekStarting: weekStarting.toISOString() } : {};
    return this.api.get<ExecutionDto[]>(
      `/executions/household/${householdId}/weekly`,
      params
    );
  }

  /**
   * Check if task completed this week
   */
  isTaskCompletedThisWeek(taskId: string): Observable<ApiResponse<boolean>> {
    return this.api.get<boolean>(
      `/executions/task/${taskId}/completed-this-week`
    );
  }

  /**
   * Get latest execution for task
   */
  getLatestExecution(taskId: string): Observable<ApiResponse<ExecutionDto>> {
    return this.api.get<ExecutionDto>(
      `/executions/task/${taskId}/latest`
    );
  }

  /**
   * Complete task (with optional photo)
   */
  completeTask(
    request: CompleteTaskRequest,
    photo?: File
  ): Observable<ApiResponse<ExecutionDto>> {
    if (photo) {
      const formData = new FormData();
      formData.append('taskId', request.taskId);
      if (request.notes) {
        formData.append('notes', request.notes);
      }
      if (request.completedAt) {
        formData.append('completedAt', request.completedAt.toISOString());
      }
      formData.append('photo', photo);

      return this.api.upload<ExecutionDto>('/executions/complete', photo, {
        taskId: request.taskId,
        notes: request.notes,
        completedAt: request.completedAt?.toISOString()
      });
    }

    return this.api.post<ExecutionDto>('/executions/complete', request);
  }

  /**
   * Update execution
   */
  updateExecution(
    id: string,
    request: UpdateExecutionRequest
  ): Observable<ApiResponse<ExecutionDto>> {
    return this.api.put<ExecutionDto>(`/executions/${id}`, request);
  }

  /**
   * Delete execution
   */
  deleteExecution(id: string): Observable<ApiResponse<any>> {
    return this.api.delete(`/executions/${id}`);
  }

  /**
   * Upload execution photo
   */
  uploadPhoto(
    id: string,
    photo: File
  ): Observable<ApiResponse<string>> {
    return this.api.upload<string>(`/executions/${id}/photo`, photo);
  }

  /**
   * Delete execution photo
   */
  deletePhoto(id: string): Observable<ApiResponse<any>> {
    return this.api.delete(`/executions/${id}/photo`);
  }
}