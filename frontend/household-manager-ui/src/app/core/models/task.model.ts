import { BaseQueryParameters } from "./api-response.model";
import { ExecutionDto } from "./execution.model";
import { RoomDto } from "./room.model";

export interface TaskDto {
  id: string;
  householdId: string;
  roomId: string;
  roomName: string;
  title: string;
  description?: string;
  type: TaskType;
  priority: TaskPriority;
  estimatedMinutes: number;
  formattedEstimatedTime: string;
  dueDate?: Date;
  scheduledWeekday?: DayOfWeek;
  assignedUserId?: string;
  assignedUserName?: string;
  isActive: boolean;
  createdAt: Date;
  isOverdue: boolean;
  isCompletedThisWeek: boolean;
  rowVersion?: Uint8Array;
}

export enum TaskType {
  Regular = 0,
  OneTime = 1
}

export enum TaskPriority {
  Low = 1,
  Medium = 2,
  High = 3
}

export enum DayOfWeek {
  Sunday = 0,
  Monday = 1,
  Tuesday = 2,
  Wednesday = 3,
  Thursday = 4,
  Friday = 5,
  Saturday = 6
}

export interface UpsertTaskRequest {
  id?: string;
  title: string;
  description?: string;
  type: TaskType;
  priority: TaskPriority;
  estimatedMinutes?: number;  // Optional - managed by backend
  roomId: string;
  assignedUserId?: string;
  isActive: boolean;
  dueDate?: Date | string;
  scheduledWeekday?: DayOfWeek;
  rowVersion?: Uint8Array;
}

export interface AssignTaskRequest {
  userId?: string;
}

export interface TaskCalendarDto {
  weekStarting: Date;
  weekEnding: Date;
  tasksByDay: { [day: number]: TaskCalendarItemDto[] };
  oneTimeTasks: TaskCalendarItemDto[];
  weeklyStats: WeeklyStatsDto;
}

export interface TaskCalendarItemDto {
  id: string;
  title: string;
  roomName: string;
  priority: TaskPriority;
  estimatedMinutes: number;
  assignedUserId?: string;
  assignedUserName?: string;
  isCompleted: boolean;
  completedAt?: Date;
  dueDate?: Date;
  isOverdue: boolean;
}

export interface WeeklyStatsDto {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  overdueTasks: number;
  completionRate: number;
}

export interface TaskQueryParameters extends BaseQueryParameters {
  householdId?: string;      // Filter by household
  roomId?: string;           // Filter by room
  type?: TaskType;           // Filter by type (Regular/OneTime)
  priority?: TaskPriority;   // Filter by priority
  assignedUserId?: string;   // Filter by assigned user
  isActive?: boolean;        // Filter by active status
  isOverdue?: boolean;       // Show only overdue tasks
  scheduledWeekday?: DayOfWeek; // Filter by weekday
}

export interface TaskDetailsDto {
  task: TaskDto;
  room: RoomDto;
  recentExecutions: ExecutionDto[];
  availableAssignees: TaskAssigneeDto[];
  permissions: TaskPermissionsDto;
  stats: TaskStatsDto;
}

export interface TaskAssigneeDto {
  userId: string;
  userName: string;
  email?: string;
  currentTaskCount: number;
}

export interface TaskPermissionsDto {
  isOwner: boolean;
  isSystemAdmin: boolean;
  isAssignedToCurrentUser: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canComplete: boolean;
  canAssign: boolean;
}

export interface TaskStatsDto {
  totalExecutions: number;
  executionsThisWeek: number;
  executionsThisMonth: number;
  lastCompleted?: Date;
  lastCompletedBy?: string;
  averageCompletionTime?: number;
}

export interface TaskAssignmentPreviewDto {
  taskId: string;
  taskTitle: string;
  priority: TaskPriority;
  roomName?: string;
  assignedUserId: string;
  assignedUserName: string;
}