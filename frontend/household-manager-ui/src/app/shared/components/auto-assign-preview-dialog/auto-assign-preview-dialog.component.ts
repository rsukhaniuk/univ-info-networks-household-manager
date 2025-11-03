import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskAssignmentPreviewDto, TaskPriority } from '../../../core/models/task.model';

@Component({
  selector: 'app-auto-assign-preview-dialog',
  imports: [CommonModule],
  templateUrl: './auto-assign-preview-dialog.component.html',
  styleUrl: './auto-assign-preview-dialog.component.scss'
})
export class AutoAssignPreviewDialogComponent {
  show = input.required<boolean>();
  preview = input.required<TaskAssignmentPreviewDto[]>();

  confirmed = output<void>();
  cancelled = output<void>();

  TaskPriority = TaskPriority;

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  getPriorityBadgeClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low:
        return 'bg-success';
      case TaskPriority.Medium:
        return 'bg-warning';
      case TaskPriority.High:
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }

  getPriorityLabel(priority: TaskPriority): string {
    return TaskPriority[priority];
  }
}
