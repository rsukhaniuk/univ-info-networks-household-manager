import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';

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

  /**
   * Export household tasks as .ics file (triggers download)
   * @param householdId Household ID
   * @param startDate Optional start date filter
   * @param endDate Optional end date filter
   */
  exportCalendar(
    householdId: string,
    startDate?: Date,
    endDate?: Date
  ): void {
    // Build URL with query parameters
    let url = `/api/households/${householdId}/calendar/export.ics`;
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

    // Trigger file download by opening URL in new window
    // The browser will automatically download the .ics file
    const token = localStorage.getItem('access_token');
    if (!token) {
      console.error('No access token found');
      return;
    }

    // Create a temporary link to trigger download with authorization
    this.downloadFileWithAuth(url, `household-tasks-${householdId}.ics`, token);
  }

  /**
   * Get calendar subscription URL and instructions
   * @param householdId Household ID
   * @returns Observable with API response containing subscription information
   */
  getSubscriptionUrl(householdId: string): Observable<ApiResponse<CalendarSubscriptionDto>> {
    return this.apiService.get<CalendarSubscriptionDto>(
      `/api/households/${householdId}/calendar/subscription`
    );
  }

  /**
   * Helper method to download file with authorization header
   * Uses Blob API to download file from authenticated endpoint
   */
  private downloadFileWithAuth(url: string, filename: string, token: string): void {
    // Get base URL from current location
    const baseUrl = window.location.origin;
    const fullUrl = baseUrl + url;

    // Fetch file with auth header
    fetch(fullUrl, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
      .then(response => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.blob();
      })
      .then(blob => {
        // Create download link
        const blobUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Clean up
        window.URL.revokeObjectURL(blobUrl);
      })
      .catch(error => {
        console.error('Error downloading calendar file:', error);
        throw error;
      });
  }
}
