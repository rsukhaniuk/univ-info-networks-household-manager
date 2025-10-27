import { BaseQueryParameters } from "./api-response.model";

export interface ExecutionDto {
  id: string;
  taskId: string;
  taskTitle: string;
  userId: string;
  userName: string;
  householdId: string;
  roomId: string;
  roomName: string;
  completedAt: Date;
  notes?: string;
  photoPath?: string;
  photoUrl?: string;
  weekStarting: Date;
  timeAgo: string;
  isThisWeek: boolean;
  hasPhoto: boolean;
}

export interface CompleteTaskRequest {
  taskId: string;
  notes?: string;
  photoPath?: string;
  completedAt?: Date;
}

export interface UpdateExecutionRequest {
  notes?: string;
  photoPath?: string;
}

export interface ExecutionQueryParameters extends BaseQueryParameters {
  householdId?: string;      // Filter by household
  taskId?: string;           // Filter by task
  userId?: string;           // Filter by user
  roomId?: string;           // Filter by room
  completedAfter?: Date;     // Completed after date
  completedBefore?: Date;    // Completed before date
  weekStarting?: Date;       // Filter by week
  thisWeekOnly?: boolean;    // Show only this week
  hasPhoto?: boolean;        // Has photo uploaded
}