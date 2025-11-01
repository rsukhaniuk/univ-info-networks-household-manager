import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  RoomDto,
  RoomWithTasksDto,
  UpsertRoomRequest,
  RoomQueryParameters
} from '../../../core/models/room.model';
import { ApiResponse, PagedResult } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class RoomService {
  private api = inject(ApiService);

  /**
   * Get paginated list of rooms for a household
   */
  getRooms(householdId: string, params?: RoomQueryParameters): Observable<ApiResponse<PagedResult<RoomDto>>> {
    return this.api.getPaged<RoomDto>(`/households/${householdId}/rooms`, params);
  }

  /**
   * Get room details with tasks
   */
  getRoomById(householdId: string, roomId: string): Observable<ApiResponse<RoomWithTasksDto>> {
    return this.api.get<RoomWithTasksDto>(`/households/${householdId}/rooms/${roomId}`);
  }

  /**
   * Create new room
   */
  createRoom(householdId: string, request: UpsertRoomRequest): Observable<ApiResponse<RoomDto>> {
    return this.api.post<RoomDto>(`/households/${householdId}/rooms`, request);
  }

  /**
   * Update room
   */
  updateRoom(householdId: string, roomId: string, request: UpsertRoomRequest): Observable<ApiResponse<RoomDto>> {
    return this.api.put<RoomDto>(`/households/${householdId}/rooms/${roomId}`, request);
  }

  /**
   * Delete room
   */
  deleteRoom(householdId: string, roomId: string): Observable<ApiResponse<any>> {
    return this.api.delete(`/households/${householdId}/rooms/${roomId}`);
  }

  /**
   * Upload room photo
   */
  uploadPhoto(householdId: string, roomId: string, file: File): Observable<ApiResponse<string>> {
    return this.api.upload<string>(`/households/${householdId}/rooms/${roomId}/photo`, file, 'photo');
  }

  /**
   * Delete room photo
   */
  deletePhoto(householdId: string, roomId: string): Observable<ApiResponse<any>> {
    return this.api.delete(`/households/${householdId}/rooms/${roomId}/photo`);
  }
}