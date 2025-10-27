import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  HouseholdDto,
  HouseholdDetailsDto,
  UpsertHouseholdRequest,
  JoinHouseholdRequest,
  HouseholdQueryParameters
} from '../../../core/models/household.model';
import { ApiResponse, PagedResult } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class HouseholdService {
  private api = inject(ApiService);
  private readonly endpoint = '/households';

  /**
   * Get paginated list of user's households
   */
  getHouseholds(params?: HouseholdQueryParameters): Observable<ApiResponse<PagedResult<HouseholdDto>>> {
    return this.api.getPaged<HouseholdDto>(this.endpoint, params);
  }

  /**
   * Get household details by ID
   */
  getHouseholdById(id: string): Observable<ApiResponse<HouseholdDetailsDto>> {
    return this.api.get<HouseholdDetailsDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Create new household
   */
  createHousehold(request: UpsertHouseholdRequest): Observable<ApiResponse<HouseholdDto>> {
    return this.api.post<HouseholdDto>(this.endpoint, request);
  }

  /**
   * Update household
   */
  updateHousehold(id: string, request: UpsertHouseholdRequest): Observable<ApiResponse<HouseholdDto>> {
    return this.api.put<HouseholdDto>(`${this.endpoint}/${id}`, request);
  }

  /**
   * Delete household
   */
  deleteHousehold(id: string): Observable<ApiResponse<any>> {
    return this.api.delete(`${this.endpoint}/${id}`);
  }

  /**
   * Get household by invite code (preview before joining)
   */
  getByInviteCode(inviteCode: string): Observable<ApiResponse<HouseholdDto>> {
    return this.api.get<HouseholdDto>(`${this.endpoint}/invite/${inviteCode}`);
  }

  /**
   * Join household using invite code
   */
  joinHousehold(request: JoinHouseholdRequest): Observable<ApiResponse<HouseholdDto>> {
    return this.api.post<HouseholdDto>(`${this.endpoint}/join`, request);
  }

  /**
   * Regenerate invite code
   */
  regenerateInviteCode(id: string): Observable<ApiResponse<string>> {
    return this.api.postEmpty<string>(`${this.endpoint}/${id}/regenerate-invite`);
  }

  /**
   * Leave household
   */
  leaveHousehold(id: string): Observable<ApiResponse<any>> {
    return this.api.postEmpty(`${this.endpoint}/${id}/leave`);
  }

  /**
   * Remove member from household (owner only)
   */
  removeMember(householdId: string, userId: string): Observable<ApiResponse<any>> {
    return this.api.delete(`${this.endpoint}/${householdId}/members/${userId}`);
  }
}