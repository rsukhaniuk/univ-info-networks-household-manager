import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { 
  UserProfileDto, 
  UserDto,
  UpdateProfileRequest,
  SetCurrentHouseholdRequest,
  UserDashboardStats 
} from '../../../core/models/user.model';
import { ApiResponse } from '../../../core/models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private api = inject(ApiService);

  /**
   * Get current user's profile with stats and households
   */
  getMyProfile(): Observable<ApiResponse<UserProfileDto>> {
    return this.api.get<UserProfileDto>('/users/me');
  }

  /**
   * Update current user's profile (firstName, lastName only)
   */
  updateMyProfile(request: UpdateProfileRequest): Observable<ApiResponse<UserDto>> {
    return this.api.put<UserDto>('/users/me', request);
  }

  /**
   * Set current active household
   */
  setCurrentHousehold(request: SetCurrentHouseholdRequest): Observable<ApiResponse<any>> {
    return this.api.put('/users/me/current-household', request);
  }

  /**
   * Get dashboard statistics
   */
  getDashboardStats(): Observable<ApiResponse<UserDashboardStats>> {
    return this.api.get<UserDashboardStats>('/users/me/dashboard');
  }
}