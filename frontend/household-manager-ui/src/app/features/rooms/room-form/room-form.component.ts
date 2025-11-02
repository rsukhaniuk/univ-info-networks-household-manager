import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { RoomService } from '../services/room.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';

@Component({
  selector: 'app-room-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './room-form.component.html',
  styleUrl: './room-form.component.scss'
})
export class RoomFormComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private roomService = inject(RoomService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private location = inject(Location);

  form!: FormGroup;
  isEditMode = false;
  roomId: string | null = null;
  householdId: string = '';
  household: any = null;
  roomName: string = '';
  isOwner = false;
  isSubmitting = false;
  error: string | null = null;

  // Photo management
  currentPhotoUrl: string | null = null;
  selectedFile: File | null = null;
  photoPreviewUrl: string | null = null;

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('id');
    this.householdId = this.route.snapshot.queryParamMap.get('householdId') || '';
    this.isEditMode = !!this.roomId;

    this.initForm();
    this.loadHousehold();

    if (this.isEditMode && this.roomId) {
      this.loadRoom(this.roomId);
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
      priority: [5, [Validators.required, Validators.min(1), Validators.max(10)]]
    });
  }

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data.household;
          this.isOwner = response.data.isOwner;

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: response.data.household.id,
            name: response.data.household.name,
            isOwner: response.data.isOwner
          });
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to load household';
      }
    });
  }

  private loadRoom(id: string): void {
    this.roomService.getRoomById(this.householdId, id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const room = response.data.room;
          this.roomName = room.name;
          this.form.patchValue({
            name: room.name,
            description: room.description,
            priority: room.priority
          });
          this.currentPhotoUrl = room.photoUrl || null;
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to load room';
      }
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
      alert('File size must be less than 5MB');
      event.target.value = '';
      return;
    }

    // Validate file type
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!validTypes.includes(file.type.toLowerCase())) {
      alert('Please select a valid image file (JPG, PNG, GIF, or WebP)');
      event.target.value = '';
      return;
    }

    this.selectedFile = file;

    // Show preview
    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.photoPreviewUrl = e.target.result;
    };
    reader.readAsDataURL(file);
  }

  clearPhotoSelection(): void {
    this.selectedFile = null;
    this.photoPreviewUrl = null;
    const fileInput = document.getElementById('photoInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const request = {
      householdId: this.householdId,
      name: this.form.value.name,
      description: this.form.value.description || undefined,
      priority: this.form.value.priority
    };

    const operation = this.isEditMode && this.roomId
      ? this.roomService.updateRoom(this.householdId, this.roomId, request)
      : this.roomService.createRoom(this.householdId, request);

    operation.subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const roomId = response.data.id;

          // Upload photo if selected
          if (this.selectedFile) {
            this.roomService.uploadPhoto(this.householdId, roomId, this.selectedFile).subscribe({
              next: () => {
                this.navigateToRoomDetails(roomId);
              },
              error: (error) => {
                // Room created/updated but photo upload failed
                console.error('Photo upload failed:', error);
                this.navigateToRoomDetails(roomId);
              }
            });
          } else {
            this.navigateToRoomDetails(roomId);
          }
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to save room';
        this.isSubmitting = false;
      }
    });
  }

  private navigateToRoomDetails(roomId: string): void {
    this.router.navigate(['/rooms', roomId], {
      queryParams: { householdId: this.householdId }
    });
  }

  onCancel(): void {
    this.location.back();
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Room' : 'Create Room';
  }

  get submitButtonText(): string {
    return this.isEditMode ? 'Update Room' : 'Create Room';
  }

  get submitButtonClass(): string {
    return this.isEditMode ? 'btn-warning' : 'btn-success';
  }

  get submitIcon(): string {
    return this.isEditMode ? 'fas fa-save' : 'fas fa-plus';
  }

  ngOnDestroy(): void {
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }

  // Form getters
  get name() {
    return this.form.get('name');
  }

  get description() {
    return this.form.get('description');
  }

  get priority() {
    return this.form.get('priority');
  }
}