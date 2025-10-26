export interface UserDto {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  fullName: string;
  profilePictureUrl?: string;
  createdAt: Date;
  role: SystemRole;
  currentHouseholdId?: string;
  isSystemAdmin: boolean;
}

export enum SystemRole {
  User = 'User',
  SystemAdmin = 'SystemAdmin'
}

export interface UserDashboardStats {
  totalHouseholds: number;
  ownedHouseholds: number;
  activeTasks: number;
  completedTasksThisWeek: number;
  lastActivity?: Date;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
}

export interface SetCurrentHouseholdRequest {
  householdId?: string;
}