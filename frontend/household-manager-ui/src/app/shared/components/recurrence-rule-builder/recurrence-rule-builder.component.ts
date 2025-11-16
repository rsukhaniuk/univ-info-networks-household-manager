import { Component, forwardRef, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS, Validator, AbstractControl, ValidationErrors } from '@angular/forms';
import { RecurrenceRuleService } from '../../services/recurrence-rule.service';

/**
 * Component for building iCalendar RRULE strings
 * Supports Daily, Weekly, Monthly, and Yearly frequencies
 */
@Component({
  selector: 'app-recurrence-rule-builder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './recurrence-rule-builder.component.html',
  styleUrls: ['./recurrence-rule-builder.component.scss'],
  providers: [
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

  // Form state
  selectedFrequency: string = 'WEEKLY';
  selectedDays: string[] = [];
  interval: number = 1;
  endDate: Date | null = null;

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
  }

  // ControlValueAccessor implementation
  writeValue(rruleString: string | null): void {
    if (!rruleString) {
      // Reset to defaults
      this.selectedFrequency = 'WEEKLY';
      this.selectedDays = ['MO'];
      this.interval = 1;
      this.endDate = null;
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

    // Ensure interval is at least 1
    if (this.interval < 1) {
      this.interval = 1;
    }

    this.updateRRule();
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
          // Default to 1st day of month
          rrule = this.recurrenceRuleService.createMonthlyRule(
            1,
            this.interval,
            this.endDate || undefined
          );
          break;

        case 'YEARLY':
          // Default to January 1st
          rrule = this.recurrenceRuleService.createYearlyRule(
            1,
            1,
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
