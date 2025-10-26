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