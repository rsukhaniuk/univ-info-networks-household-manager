import { Injectable } from '@angular/core';
import { RRule, Frequency, Weekday, ByWeekday } from 'rrule';

/**
 * Service for handling iCalendar RRULE (Recurrence Rule) generation and parsing
 * Supports RFC 5545 recurrence rules for task scheduling
 */
@Injectable({
  providedIn: 'root'
})
export class RecurrenceRuleService {

  // Weekday mapping for RRULE library
  private readonly weekdayMap: { [key: string]: Weekday } = {
    'MO': RRule.MO,
    'TU': RRule.TU,
    'WE': RRule.WE,
    'TH': RRule.TH,
    'FR': RRule.FR,
    'SA': RRule.SA,
    'SU': RRule.SU
  };

  // Human-readable weekday names
  private readonly weekdayNames: { [key: string]: string } = {
    'MO': 'Monday',
    'TU': 'Tuesday',
    'WE': 'Wednesday',
    'TH': 'Thursday',
    'FR': 'Friday',
    'SA': 'Saturday',
    'SU': 'Sunday'
  };

  /**
   * Format RRULE string to human-readable text
   * @param rruleString RRULE string (e.g., "FREQ=WEEKLY;BYDAY=MO,WE,FR")
   * @returns Human-readable text (e.g., "Every Monday, Wednesday, and Friday")
   */
  formatRule(rruleString: string | null | undefined): string {
    if (!rruleString) {
      return 'N/A';
    }

    try {
      // Parse RRULE string
      const rule = RRule.fromString(rruleString);

      // Use rrule.js built-in text generation (English)
      return rule.toText();
    } catch (error) {
      console.warn('Invalid RRULE format:', rruleString, error);
      return 'Invalid recurrence rule';
    }
  }

  /**
   * Create a weekly recurrence rule
   * @param days Array of weekday codes (e.g., ['MO', 'WE', 'FR'])
   * @param interval Interval between occurrences (default: 1 = every week)
   * @param endDate Optional end date for recurrence
   * @returns RRULE string
   */
  createWeeklyRule(days: string[], interval: number = 1, endDate?: Date): string {
    if (!days || days.length === 0) {
      throw new Error('At least one weekday must be specified');
    }

    // Convert day codes to RRule Weekday objects
    const weekdays = days.map(day => {
      const weekday = this.weekdayMap[day.toUpperCase()];
      if (!weekday) {
        throw new Error(`Invalid weekday code: ${day}`);
      }
      return weekday;
    });

    // Build RRULE options
    const options: any = {
      freq: RRule.WEEKLY,
      byweekday: weekdays,
      interval: interval
    };

    // Add end date if specified
    if (endDate) {
      options.until = endDate;
    }

    // Generate RRULE
    const rule = new RRule(options);

    // Return only the RRULE part (without DTSTART)
    return rule.toString().split('\n').find(line => line.startsWith('RRULE:'))?.replace('RRULE:', '') || '';
  }

  /**
   * Create a daily recurrence rule
   * @param interval Interval between occurrences (default: 1 = every day)
   * @param endDate Optional end date for recurrence
   * @returns RRULE string
   */
  createDailyRule(interval: number = 1, endDate?: Date): string {
    const options: any = {
      freq: RRule.DAILY,
      interval: interval
    };

    if (endDate) {
      options.until = endDate;
    }

    const rule = new RRule(options);
    return rule.toString().split('\n').find(line => line.startsWith('RRULE:'))?.replace('RRULE:', '') || '';
  }

  /**
   * Create a monthly recurrence rule
   * @param dayOfMonth Day of month (1-31)
   * @param interval Interval between occurrences (default: 1 = every month)
   * @param endDate Optional end date for recurrence
   * @returns RRULE string
   */
  createMonthlyRule(dayOfMonth: number, interval: number = 1, endDate?: Date): string {
    const options: any = {
      freq: RRule.MONTHLY,
      bymonthday: [dayOfMonth],
      interval: interval
    };

    if (endDate) {
      options.until = endDate;
    }

    const rule = new RRule(options);
    return rule.toString().split('\n').find(line => line.startsWith('RRULE:'))?.replace('RRULE:', '') || '';
  }

  /**
   * Create a yearly recurrence rule
   * @param month Month (1-12)
   * @param dayOfMonth Day of month (1-31)
   * @param interval Interval between occurrences (default: 1 = every year)
   * @param endDate Optional end date for recurrence
   * @returns RRULE string
   */
  createYearlyRule(month: number, dayOfMonth: number, interval: number = 1, endDate?: Date): string {
    const options: any = {
      freq: RRule.YEARLY,
      bymonth: [month],
      bymonthday: [dayOfMonth],
      interval: interval
    };

    if (endDate) {
      options.until = endDate;
    }

    const rule = new RRule(options);
    return rule.toString().split('\n').find(line => line.startsWith('RRULE:'))?.replace('RRULE:', '') || '';
  }

  /**
   * Parse RRULE string to RRule object
   * @param rruleString RRULE string
   * @returns RRule object or null if invalid
   */
  parseRule(rruleString: string | null | undefined): RRule | null {
    if (!rruleString) {
      return null;
    }

    try {
      return RRule.fromString(rruleString);
    } catch (error) {
      console.warn('Failed to parse RRULE:', rruleString, error);
      return null;
    }
  }

  /**
   * Validate RRULE string
   * @param rruleString RRULE string
   * @returns true if valid, false otherwise
   */
  isValidRule(rruleString: string | null | undefined): boolean {
    if (!rruleString) {
      return false;
    }

    try {
      RRule.fromString(rruleString);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Extract weekdays from RRULE string
   * @param rruleString RRULE string
   * @returns Array of weekday codes (e.g., ['MO', 'WE', 'FR']) or empty array
   */
  extractWeekdays(rruleString: string | null | undefined): string[] {
    const rule = this.parseRule(rruleString);
    if (!rule) {
      return [];
    }

    // Get weekdays from rule options
    const weekdays = rule.options.byweekday;
    if (!weekdays || !Array.isArray(weekdays)) {
      return [];
    }

    // Convert Weekday objects to string codes
    return weekdays.map((wd: any) => {
      // Handle both Weekday objects and plain numbers
      const dayNum = typeof wd === 'number' ? wd : wd.weekday;

      // Map day number to code (0=MO, 1=TU, ...)
      const dayMap = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU'];
      return dayMap[dayNum];
    }).filter(Boolean);
  }

  /**
   * Get frequency from RRULE string
   * @param rruleString RRULE string
   * @returns Frequency string ('DAILY', 'WEEKLY', 'MONTHLY', 'YEARLY') or null
   */
  getFrequency(rruleString: string | null | undefined): string | null {
    const rule = this.parseRule(rruleString);
    if (!rule) {
      return null;
    }

    const freqMap: { [key: number]: string } = {
      [RRule.DAILY]: 'DAILY',
      [RRule.WEEKLY]: 'WEEKLY',
      [RRule.MONTHLY]: 'MONTHLY',
      [RRule.YEARLY]: 'YEARLY'
    };

    return freqMap[rule.options.freq] || null;
  }

  /**
   * Get interval from RRULE string
   * @param rruleString RRULE string
   * @returns Interval number or 1 if not specified
   */
  getInterval(rruleString: string | null | undefined): number {
    const rule = this.parseRule(rruleString);
    if (!rule) {
      return 1;
    }

    return rule.options.interval || 1;
  }

  /**
   * Get end date from RRULE string
   * @param rruleString RRULE string
   * @returns End date or null
   */
  getEndDate(rruleString: string | null | undefined): Date | null {
    const rule = this.parseRule(rruleString);
    if (!rule || !rule.options.until) {
      return null;
    }

    return rule.options.until;
  }

  /**
   * Format weekday codes to human-readable text
   * @param dayCodes Array of weekday codes (e.g., ['MO', 'WE'])
   * @returns Formatted string (e.g., "Monday, Wednesday")
   */
  formatWeekdays(dayCodes: string[]): string {
    if (!dayCodes || dayCodes.length === 0) {
      return 'No days selected';
    }

    const dayNames = dayCodes.map(code => this.weekdayNames[code.toUpperCase()]).filter(Boolean);

    if (dayNames.length === 0) {
      return 'Invalid days';
    }

    if (dayNames.length === 1) {
      return dayNames[0];
    }

    // Join with commas and "and" for last item
    const allButLast = dayNames.slice(0, -1).join(', ');
    const last = dayNames[dayNames.length - 1];
    return `${allButLast} and ${last}`;
  }
}
