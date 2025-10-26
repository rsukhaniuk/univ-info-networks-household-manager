import { ExecutionDto } from "./execution.model";
import { TaskDto } from "./task.model";

export interface RoomDto {
  id: string;
  householdId: string;
  name: string;
  description?: string;
  photoPath?: string;
  photoUrl?: string;
  priority: number;
  createdAt: Date;
  activeTaskCount: number;
  hasPhoto: boolean;
}

export interface RoomWithTasksDto {
  room: RoomDto;
  activeTasks: TaskDto[];
  recentExecutions: ExecutionDto[];
  isOwner: boolean;
  stats: RoomStatsDto;
}

export interface RoomStatsDto {
  totalTasks: number;
  activeTasks: number;
  overdueTasks: number;
  completedThisWeek: number;
  averageCompletionTime?: number;
  lastActivity?: Date;
}

export interface UpsertRoomRequest {
  id?: string;
  householdId: string;
  name: string;
  description?: string;
  priority: number;
  photoPath?: string;
}