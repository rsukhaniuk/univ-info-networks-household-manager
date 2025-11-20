import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';

/**
 * DTO for calendar subscription information
 */
export interface CalendarSubscriptionDto {
  subscriptionUrl: string;
  householdId: string;
  calendarName: string;
  instructions: string;
  generatedAt: Date;
}

/**
 * Service for calendar export and subscription operations
 */
@Injectable({
  providedIn: 'root'
})
export class CalendarService {
  private apiService = inject(ApiService);
  private http = inject(HttpClient);

  /**
   * Export household tasks as .ics file (triggers download)
   * @param householdId Household ID
   * @param startDate Optional start date filter
   * @param endDate Optional end date filter
   * @returns Promise that resolves when download completes
   */
  exportCalendar(
    householdId: string,
    startDate?: Date,
    endDate?: Date
  ): Promise<void> {
    // Build URL with query parameters
    let url = `${environment.apiUrl}/households/${householdId}/calendar/export.ics`;
    const params: string[] = [];

    if (startDate) {
      params.push(`startDate=${startDate.toISOString()}`);
    }
    if (endDate) {
      params.push(`endDate=${endDate.toISOString()}`);
    }

    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    // Use HttpClient to download file (error interceptor will handle errors including blob errors)
    return new Promise((resolve, reject) => {
      this.http.get(url, {
        responseType: 'blob',
        observe: 'response'
      }).subscribe({
        next: (response) => {
          const blob = response.body;
          if (!blob || blob.size === 0) {
            reject(new Error('Downloaded file is empty'));
            return;
          }

          // Create download link
          const blobUrl = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = blobUrl;
          link.download = `household-tasks-${householdId}.ics`;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);

          // Clean up
          window.URL.revokeObjectURL(blobUrl);
          resolve();
        },
        error: (error) => {
          // Error will be handled by error interceptor (which now supports blob errors)
          reject(error);
        }
      });
    });
  }

  /**
   * Get calendar subscription URL and instructions
   * @param householdId Household ID
   * @returns Observable with API response containing subscription information
   */
  getSubscriptionUrl(householdId: string): Observable<ApiResponse<CalendarSubscriptionDto>> {
    return this.apiService.get<CalendarSubscriptionDto>(
      `/households/${householdId}/calendar/subscription`
    );
  }

}
