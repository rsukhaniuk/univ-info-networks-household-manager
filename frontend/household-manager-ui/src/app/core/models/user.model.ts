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

export interface UserHouseholdDto {
  householdId: string;
  householdName: string;
  role: string;
  joinedAt: string;
  activeTaskCount: number;
  isCurrent: boolean;
}

export interface UserProfileDto {
  user: UserDto;
  stats: UserDashboardStats;
  households: ReadonlyArray<UserHouseholdDto>;
}

export interface UserQueryParameters {
  role?: SystemRole;
  householdId?: string;
  createdAfter?: string;
  createdBefore?: string;
  hasActiveTasks?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}