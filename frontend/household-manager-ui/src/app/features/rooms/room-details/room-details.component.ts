import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { RoomService } from '../services/room.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
import { RoomWithTasksDto } from '../../../core/models/room.model';
import { HouseholdDto } from '../../../core/models/household.model';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-room-details',
  standalone: true,
  imports: [CommonModule, RouterModule, TableModule, ConfirmationDialogComponent, UtcDatePipe],
  templateUrl: './room-details.component.html',
  styleUrl: './room-details.component.scss',
})
export class RoomDetailsComponent implements OnInit, OnDestroy {
  private roomService = inject(RoomService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);
  private toastService = inject(ToastService);

  roomId: string = '';
  householdId: string = '';
  household: HouseholdDto | null = null;
  roomDetails: RoomWithTasksDto | null = null;
  isSystemAdmin = false;
  canManageRoom = false;
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Modal state
  showPhotoModal = false;
  selectedPhotoFile: File | null = null;
  isUploadingPhoto = false;

  // Confirmation dialog state
  showConfirmDialog = false;
  confirmDialogData: ConfirmDialogData = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'danger'
  };
  private pendingAction: (() => void) | null = null;

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('id') || '';
    this.householdId = this.route.snapshot.queryParamMap.get('householdId') || '';

    if (this.roomId && this.householdId) {
      this.auth.isSystemAdmin$().subscribe((isAdmin) => {
        this.isSystemAdmin = isAdmin;
      });

      this.loadHousehold();
      this.loadRoom();
    }
  }

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data.household;

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: response.data.household.id,
            name: response.data.household.name,
            isOwner: response.data.isOwner
          });
        }
      },
      error: (error) => {
        console.error('Failed to load household:', error);
      }
    });
  }

  loadRoom(): void {
    this.isLoading = true;
    this.error = null;

    this.roomService.getRoomById(this.householdId, this.roomId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.roomDetails = response.data;
          this.updateCanManageRoom();
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load room details';
        this.isLoading = false;
      },
    });
  }

  private updateCanManageRoom(): void {
    this.canManageRoom = (this.roomDetails?.isOwner ?? false) || this.isSystemAdmin;
  }

  openPhotoModal(): void {
    this.showPhotoModal = true;
    this.selectedPhotoFile = null;
  }

  closePhotoModal(): void {
    this.showPhotoModal = false;
    this.selectedPhotoFile = null;
  }

  onPhotoFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    // Validate
    if (file.size > 5 * 1024 * 1024) {
      alert('File size must be less than 5MB');
      return;
    }

    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!validTypes.includes(file.type.toLowerCase())) {
      alert('Please select a valid image file');
      return;
    }

    this.selectedPhotoFile = file;
  }

  uploadPhoto(): void {
    if (!this.selectedPhotoFile) return;

    this.isUploadingPhoto = true;
    this.roomService.uploadPhoto(this.householdId, this.roomId, this.selectedPhotoFile).subscribe({
      next: () => {
        this.successMessage = 'Photo uploaded successfully';
        this.closePhotoModal();
        this.loadRoom();
        this.autoHideMessage();
        this.isUploadingPhoto = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to upload photo';
        this.isUploadingPhoto = false;
      },
    });
  }

  deletePhoto(): void {
    if (!confirm('Are you sure you want to delete this photo?')) return;

    this.roomService.deletePhoto(this.householdId, this.roomId).subscribe({
      next: () => {
        this.successMessage = 'Photo deleted successfully';
        this.closePhotoModal();
        this.loadRoom();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete photo';
      },
    });
  }

  confirmDeleteRoom(): void {
    this.showConfirmDialog = true;

    this.confirmDialogData = {
      title: 'Delete Room',
      message: `Are you sure you want to delete "${this.roomDetails?.room?.name}"?\n\nThis action cannot be undone. All tasks and execution history in this room will be permanently deleted.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-trash-alt',
      iconClass: 'text-danger'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;

      this.roomService.deleteRoom(this.householdId, this.roomId).subscribe({
        next: () => {
          this.toastService.success(`Room "${this.roomDetails?.room?.name}" deleted successfully`);
          setTimeout(() => {
            this.router.navigate(['/rooms'], {
              queryParams: { householdId: this.householdId },
            });
          }, 150);
        },
        error: (error) => {
          console.error('Failed to delete room:', error);
        },
      });
    };
  }

  onDialogConfirmed(): void {
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }

  onDialogCancelled(): void {
    this.pendingAction = null;
    this.showConfirmDialog = false;
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }

  getWeekdayName(weekday: number | null | undefined): string {
    if (weekday === null || weekday === undefined) return 'N/A';
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[weekday] || 'N/A';
  }

  ngOnDestroy(): void {
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }
}
