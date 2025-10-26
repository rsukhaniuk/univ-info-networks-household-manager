import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /**
   * GET request
   */
  get<T>(endpoint: string, params?: any): Observable<ApiResponse<T>> {
    const httpParams = this.buildParams(params);
    return this.http.get<ApiResponse<T>>(`${this.baseUrl}${endpoint}`, { params: httpParams });
  }

  /**
   * GET request for paged results
   */
  getPaged<T>(endpoint: string, params?: any): Observable<ApiResponse<PagedResult<T>>> {
    const httpParams = this.buildParams(params);
    return this.http.get<ApiResponse<PagedResult<T>>>(`${this.baseUrl}${endpoint}`, { params: httpParams });
  }

  /**
   * GET request without ApiResponse wrapper (for simple endpoints)
   */
  getRaw<T>(endpoint: string, params?: any): Observable<T> {
    const httpParams = this.buildParams(params);
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, { params: httpParams });
  }

  /**
   * POST request
   */
  post<T>(endpoint: string, body: any): Observable<ApiResponse<T>> {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${endpoint}`, body);
  }

  /**
   * POST request without body
   */
  postEmpty<T>(endpoint: string): Observable<ApiResponse<T>> {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${endpoint}`, {});
  }

  /**
   * PUT request
   */
  put<T>(endpoint: string, body: any): Observable<ApiResponse<T>> {
    return this.http.put<ApiResponse<T>>(`${this.baseUrl}${endpoint}`, body);
  }

  /**
   * DELETE request
   */
  delete<T>(endpoint: string): Observable<ApiResponse<T>> {
    return this.http.delete<ApiResponse<T>>(`${this.baseUrl}${endpoint}`);
  }

  /**
   * Upload file (multipart/form-data)
   */
  upload<T>(endpoint: string, file: File, additionalData?: any): Observable<ApiResponse<T>> {
    const formData = new FormData();
    formData.append('file', file);

    if (additionalData) {
      Object.keys(additionalData).forEach(key => {
        formData.append(key, additionalData[key]);
      });
    }

    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${endpoint}`, formData);
  }

  /**
   * Build HttpParams from object
   */
  private buildParams(params?: any): HttpParams {
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach(key => {
        const value = params[key];
        if (value !== null && value !== undefined) {
          if (Array.isArray(value)) {
            value.forEach(item => {
              httpParams = httpParams.append(key, item.toString());
            });
          } else {
            httpParams = httpParams.append(key, value.toString());
          }
        }
      });
    }

    return httpParams;
  }
}