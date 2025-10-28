import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { RoomService } from '../services/room.service';
import { RoomWithTasksDto } from '../../../core/models/room.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-room-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './room-details.component.html',
  styleUrl: './room-details.component.scss',
})
export class RoomDetailsComponent implements OnInit {
  private roomService = inject(RoomService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  roomId: string = '';
  householdId: string = '';
  roomDetails: RoomWithTasksDto | null = null;
  isSystemAdmin = false;
  canManageRoom = false;
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Modal state
  showPhotoModal = false;
  showDeleteModal = false;
  selectedPhotoFile: File | null = null;
  isUploadingPhoto = false;

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('id') || '';
    this.householdId = this.route.snapshot.queryParamMap.get('householdId') || '';

    if (this.roomId && this.householdId) {
      // Check if SystemAdmin
      this.authService.isSystemAdmin$().subscribe((isAdmin) => {
        this.isSystemAdmin = isAdmin;
        this.updateCanManageRoom();
      });

      this.loadRoom();
    }
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

  openDeleteModal(): void {
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
  }

  confirmDelete(): void {
    this.roomService.deleteRoom(this.householdId, this.roomId).subscribe({
      next: () => {
        this.router.navigate(['/rooms'], {
          queryParams: { householdId: this.householdId },
        });
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete room';
        this.closeDeleteModal();
      },
    });
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }
}
