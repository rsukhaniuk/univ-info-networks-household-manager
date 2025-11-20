import { Component, forwardRef, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS, Validator, AbstractControl, ValidationErrors } from '@angular/forms';
import { FlatpickrDirective, FlatpickrDefaults } from 'angularx-flatpickr';
import { english } from 'flatpickr/dist/l10n/default';
import { RecurrenceRuleService } from '../../services/recurrence-rule.service';

/**
 * Component for building iCalendar RRULE strings
 * Supports Daily, Weekly, Monthly, and Yearly frequencies
 */
@Component({
  selector: 'app-recurrence-rule-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, FlatpickrDirective],
  templateUrl: './recurrence-rule-builder.component.html',
  styleUrls: ['./recurrence-rule-builder.component.scss'],
  providers: [
    FlatpickrDefaults,
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RecurrenceRuleBuilderComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => RecurrenceRuleBuilderComponent),
      multi: true
    }
  ]
})
export class RecurrenceRuleBuilderComponent implements OnInit, ControlValueAccessor, Validator {
  @Input() required: boolean = false;

  // Frequency options
  frequencies = [
    { value: 'DAILY', label: 'Daily' },
    { value: 'WEEKLY', label: 'Weekly' },
    { value: 'MONTHLY', label: 'Monthly' },
    { value: 'YEARLY', label: 'Yearly' }
  ];

  // Weekday options for weekly frequency
  weekdays = [
    { code: 'MO', label: 'Mon' },
    { code: 'TU', label: 'Tue' },
    { code: 'WE', label: 'Wed' },
    { code: 'TH', label: 'Thu' },
    { code: 'FR', label: 'Fri' },
    { code: 'SA', label: 'Sat' },
    { code: 'SU', label: 'Sun' }
  ];

  // Month options for yearly frequency
  months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' }
  ];

  // Month day options for monthly frequency (including "last day of month")
  monthDayOptions = [
    { value: -1, label: 'Last day of month' },
    ...Array.from({ length: 31 }, (_, i) => ({
      value: i + 1,
      label: `Day ${i + 1}`
    }))
  ];

  // Form state
  selectedFrequency: string = 'WEEKLY';
  selectedDays: string[] = [];
  interval: number = 1;
  endDate: Date | null = null;
  monthDay: number = 1; // Day of month for MONTHLY (1-31)
  yearMonth: number = 1; // Month for YEARLY (1-12)
  yearDay: number = 1; // Day of month for YEARLY (1-31)

  // Flatpickr options for end date (date only, no time)
  flatpickrEndDateOptions: any = {
    enableTime: false,
    dateFormat: 'Y-m-d',
    minDate: 'today',
    altInput: true,
    altFormat: 'j F Y',
    allowInput: false,
    locale: {
      ...english,
      firstDayOfWeek: 1
    },
    allowClear: true
  };

  // ControlValueAccessor
  value: string | null = null;
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};
  disabled: boolean = false;
  touched: boolean = false;

  constructor(private recurrenceRuleService: RecurrenceRuleService) {}

  ngOnInit(): void {
    // Default to Monday if weekly and no days selected
    if (this.selectedFrequency === 'WEEKLY' && this.selectedDays.length === 0) {
      this.selectedDays = ['MO'];
    }

    // Generate initial RRULE with default values
    this.updateRRule();
  }

  // ControlValueAccessor implementation
  writeValue(rruleString: string | null): void {
    if (!rruleString) {
      // Reset to defaults
      this.selectedFrequency = 'WEEKLY';
      this.selectedDays = ['MO'];
      this.interval = 1;
      this.endDate = null;
      this.monthDay = 1;
      this.yearMonth = 1;
      this.yearDay = 1;
      this.value = null;
      return;
    }

    // Parse existing RRULE
    const frequency = this.recurrenceRuleService.getFrequency(rruleString);
    if (frequency) {
      this.selectedFrequency = frequency;
    }

    this.interval = this.recurrenceRuleService.getInterval(rruleString);
    this.endDate = this.recurrenceRuleService.getEndDate(rruleString);

    if (frequency === 'WEEKLY') {
      const days = this.recurrenceRuleService.extractWeekdays(rruleString);
      if (days.length > 0) {
        this.selectedDays = days;
      }
    } else if (frequency === 'MONTHLY') {
      const monthDayMatch = rruleString.match(/BYMONTHDAY=(\d+)/);
      if (monthDayMatch) {
        this.monthDay = parseInt(monthDayMatch[1], 10);
      }
    } else if (frequency === 'YEARLY') {
      const monthMatch = rruleString.match(/BYMONTH=(\d+)/);
      const dayMatch = rruleString.match(/BYMONTHDAY=(\d+)/);
      if (monthMatch) {
        this.yearMonth = parseInt(monthMatch[1], 10);
      }
      if (dayMatch) {
        this.yearDay = parseInt(dayMatch[1], 10);
      }
    }

    this.value = rruleString;
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // Validator implementation
  validate(control: AbstractControl): ValidationErrors | null {
    if (!this.required) {
      return null;
    }

    if (!this.value || this.value.trim() === '') {
      return { required: true };
    }

    if (this.selectedFrequency === 'WEEKLY' && this.selectedDays.length === 0) {
      return { noWeekdaysSelected: true };
    }

    return null;
  }

  // Event handlers
  onFrequencyChange(): void {
    this.markAsTouched();

    // Reset days when switching to/from weekly
    if (this.selectedFrequency === 'WEEKLY' && this.selectedDays.length === 0) {
      this.selectedDays = ['MO'];
    }

    this.updateRRule();
  }

  onDayToggle(dayCode: string): void {
    this.markAsTouched();

    const index = this.selectedDays.indexOf(dayCode);
    if (index > -1) {
      this.selectedDays.splice(index, 1);
    } else {
      this.selectedDays.push(dayCode);
    }

    // Sort days in week order
    const weekOrder = ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU'];
    this.selectedDays.sort((a, b) => weekOrder.indexOf(a) - weekOrder.indexOf(b));

    this.updateRRule();
  }

  isDaySelected(dayCode: string): boolean {
    return this.selectedDays.includes(dayCode);
  }

  onIntervalChange(): void {
    this.markAsTouched();

    // Set reasonable limits based on frequency
    const maxInterval = this.getMaxInterval();

    if (this.interval < 1) {
      this.interval = 1;
    } else if (this.interval > maxInterval) {
      this.interval = maxInterval;
    }

    this.updateRRule();
  }

  // Get maximum interval based on frequency
  getMaxInterval(): number {
    switch (this.selectedFrequency) {
      case 'DAILY':
        return 730; // Max: every 730 days (~2 years)
      case 'WEEKLY':
        return 104; // Max: every 104 weeks (~2 years)
      case 'MONTHLY':
        return 60; // Max: every 60 months (5 years)
      case 'YEARLY':
        return 50; // Max: every 50 years
      default:
        return 100;
    }
  }

  onMonthDayChange(): void {
    this.markAsTouched();

    // Ensure monthDay is either -1 (last day of month) or between 1 and 31
    if (this.monthDay < -1) {
      this.monthDay = -1;
    } else if (this.monthDay === 0) {
      this.monthDay = 1;
    } else if (this.monthDay > 31) {
      this.monthDay = 31;
    }

    this.updateRRule();
  }

  onYearMonthChange(): void {
    this.markAsTouched();

    // Ensure yearMonth is between 1 and 12
    if (this.yearMonth < 1) {
      this.yearMonth = 1;
    } else if (this.yearMonth > 12) {
      this.yearMonth = 12;
    }

    // Validate yearDay based on selected month
    const maxDayInMonth = this.getMaxDayInMonth(this.yearMonth);
    if (this.yearDay > maxDayInMonth) {
      this.yearDay = maxDayInMonth;
    }

    this.updateRRule();
  }

  onYearDayChange(): void {
    this.markAsTouched();

    // Ensure yearDay is valid for the selected month
    const maxDayInMonth = this.getMaxDayInMonth(this.yearMonth);
    if (this.yearDay < 1) {
      this.yearDay = 1;
    } else if (this.yearDay > maxDayInMonth) {
      this.yearDay = maxDayInMonth;
    }

    this.updateRRule();
  }

  // Get max day in month (considering February as 29 for leap years)
  getMaxDayInMonth(month: number): number {
    const daysInMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    return daysInMonth[month - 1] || 31;
  }

  onEndDateChange(): void {
    this.markAsTouched();
    this.updateRRule();
  }

  private markAsTouched(): void {
    if (!this.touched) {
      this.touched = true;
      this.onTouched();
    }
  }

  private updateRRule(): void {
    try {
      let rrule: string = '';

      switch (this.selectedFrequency) {
        case 'DAILY':
          rrule = this.recurrenceRuleService.createDailyRule(
            this.interval,
            this.endDate || undefined
          );
          break;

        case 'WEEKLY':
          if (this.selectedDays.length > 0) {
            rrule = this.recurrenceRuleService.createWeeklyRule(
              this.selectedDays,
              this.interval,
              this.endDate || undefined
            );
          }
          break;

        case 'MONTHLY':
          rrule = this.recurrenceRuleService.createMonthlyRule(
            this.monthDay,
            this.interval,
            this.endDate || undefined
          );
          break;

        case 'YEARLY':
          rrule = this.recurrenceRuleService.createYearlyRule(
            this.yearMonth,
            this.yearDay,
            this.interval,
            this.endDate || undefined
          );
          break;
      }

      this.value = rrule || null;
      this.onChange(this.value);
    } catch (error) {
      console.error('Error generating RRULE:', error);
      this.value = null;
      this.onChange(null);
    }
  }

  // Helper to display frequency label
  getFrequencyLabel(): string {
    if (this.interval === 1) {
      switch (this.selectedFrequency) {
        case 'DAILY': return 'day';
        case 'WEEKLY': return 'week';
        case 'MONTHLY': return 'month';
        case 'YEARLY': return 'year';
        default: return '';
      }
    } else {
      switch (this.selectedFrequency) {
        case 'DAILY': return 'days';
        case 'WEEKLY': return 'weeks';
        case 'MONTHLY': return 'months';
        case 'YEARLY': return 'years';
        default: return '';
      }
    }
  }

  // Generate preview text
  getPreviewText(): string {
    if (!this.value) {
      return 'No recurrence pattern set';
    }

    return this.recurrenceRuleService.formatRule(this.value);
  }
}
