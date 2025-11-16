import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarSubscriptionDto } from '../../../features/tasks/services/calendar.service';

@Component({
  selector: 'app-calendar-subscription-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendar-subscription-dialog.component.html',
  styleUrls: ['./calendar-subscription-dialog.component.scss']
})
export class CalendarSubscriptionDialogComponent {
  @Input() show: boolean = false;
  @Input() subscriptionData: CalendarSubscriptionDto | null = null;
  @Output() close = new EventEmitter<void>();

  copySuccess: boolean = false;

  onClose(): void {
    this.copySuccess = false;
    this.close.emit();
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copySuccess = true;
      setTimeout(() => {
        this.copySuccess = false;
      }, 2000);
    }).catch(err => {
      console.error('Failed to copy:', err);
    });
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
