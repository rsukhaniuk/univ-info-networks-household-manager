  import { BaseQueryParameters } from "./api-response.model";
  import { RoomDto } from "./room.model";
  import { TaskDto } from "./task.model";

  export interface HouseholdDto {
    id: string;
    name: string;
    description?: string;
    inviteCode: string;
    inviteCodeExpiresAt?: Date;
    createdAt: Date;
    memberCount: number;
    activeTaskCount: number;
    roomCount: number;

    role?: HouseholdRole;
  }

  export interface HouseholdDetailsDto {
    household: HouseholdDto;
    rooms: RoomDto[];
    activeTasks: TaskDto[];
    members: HouseholdMemberDto[];
    isOwner: boolean;
    taskCountsByUser: { [userId: string]: number };
  }

  export interface HouseholdMemberDto {
    id: string;
    userId: string;
    householdId: string;
    userName: string;
    email?: string;
    role: HouseholdRole;
    joinedAt: Date;
    activeTaskCount: number;
    completedThisWeek: number;
  }

  export enum HouseholdRole {
    Member = 'Member',  
    Owner = 'Owner'   
  }

  export interface UpsertHouseholdRequest {
    id?: string;
    name: string;
    description?: string;
  }

  export interface JoinHouseholdRequest {
    inviteCode: string;
  }

  export interface RegenerateInviteCodeResponse {
    inviteCode: string;
    inviteCodeExpiresAt?: Date;
  }

  export interface HouseholdQueryParameters extends BaseQueryParameters {
    userId?: string;           // Filter by user membership
    ownedByUser?: boolean;     // Show only owned households
    minMembers?: number;       // Minimum member count
    maxMembers?: number;       // Maximum member count
  }