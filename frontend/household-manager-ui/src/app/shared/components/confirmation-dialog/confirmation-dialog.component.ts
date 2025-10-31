import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmClass?: 'danger' | 'warning' | 'primary' | 'success';
  icon?: string;
  iconClass?: string;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirmation-dialog.component.html',
  styleUrls: ['./confirmation-dialog.component.scss']
})
export class ConfirmationDialogComponent {
  @Input() data: ConfirmDialogData = {
    title: 'Confirm Action',
    message: 'Are you sure you want to proceed?',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'danger',
    icon: 'fa-exclamation-triangle',
    iconClass: 'text-warning'
  };

  @Input() show = false;
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  onConfirm(): void {
    this.confirmed.emit();
    this.show = false;
  }

  onCancel(): void {
    this.cancelled.emit();
    this.show = false;
  }

  onBackdropClick(event: MouseEvent): void {
    // Закриваємо тільки при кліку на backdrop, не на modal-dialog
    if ((event.target as HTMLElement).classList.contains('modal')) {
      this.onCancel();
    }
  }
}