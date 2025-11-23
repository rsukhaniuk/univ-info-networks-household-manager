import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Pipe to convert relative photo paths to absolute URLs
 * Uses apiUrl base to construct full URL for photos served by backend
 */
@Pipe({
  name: 'photoUrl',
  standalone: true
})
export class PhotoUrlPipe implements PipeTransform {
  private readonly apiBaseUrl: string;

  constructor() {
    // Extract base URL from apiUrl (remove /api suffix)
    this.apiBaseUrl = environment.apiUrl.replace(/\/api\/?$/, '');
  }

  transform(photoUrl: string | null | undefined): string | null {
    if (!photoUrl) {
      return null;
    }

    // If already absolute URL, return as is
    if (photoUrl.startsWith('http://') || photoUrl.startsWith('https://')) {
      return photoUrl;
    }

    // Ensure path starts with /
    const path = photoUrl.startsWith('/') ? photoUrl : `/${photoUrl}`;

    return `${this.apiBaseUrl}${path}`;
  }
}
