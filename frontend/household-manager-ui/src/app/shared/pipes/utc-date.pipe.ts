import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

/**
 * Converts UTC date string to local time and formats it
 * Usage: {{ utcDateString | utcDate:'medium' }}
 */
@Pipe({
  name: 'utcDate',
  standalone: true
})
export class UtcDatePipe implements PipeTransform {
  private datePipe = new DatePipe('en-US');

  transform(value: string | Date | null | undefined, format: string = 'medium'): string | null {
    if (!value) return null;

    let date: Date;
    
    if (typeof value === 'string') {
      // If string doesn't end with 'Z', assume it's UTC and add 'Z'
      const dateString = value.endsWith('Z') ? value : value + 'Z';
      date = new Date(dateString);
    } else {
      date = value;
    }
    
    // Check if valid date
    if (isNaN(date.getTime())) return null;

    // Convert format to 24-hour format
    const format24h = this.convertTo24HourFormat(format);

    // Format date in local timezone (default behavior)
    return this.datePipe.transform(date, format24h);
  }

  private convertTo24HourFormat(format: string): string {
    // Map common formats to 24-hour equivalents
    const formatMap: { [key: string]: string } = {
      'short': 'M/d/yy, HH:mm',           // 11/1/24, 14:30
      'medium': 'MMM d, y, HH:mm:ss',     // Nov 1, 2024, 14:30:45
      'long': 'MMMM d, y, HH:mm:ss',      // November 1, 2024, 14:30:45
      'full': 'EEEE, MMMM d, y, HH:mm:ss' // Friday, November 1, 2024, 14:30:45
    };

    return formatMap[format] || format;
  }
}
