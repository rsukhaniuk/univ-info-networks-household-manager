import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { RoomService } from '../services/room.service';
import { HouseholdService } from '../../households/services/household.service';
import { RoomDto } from '../../../core/models/room.model';
import { HouseholdDto, HouseholdDetailsDto } from '../../../core/models/household.model';
import { AuthService } from '../../../core/services/auth.service';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [CommonModule, RouterModule, UtcDatePipe],
  templateUrl: './room-list.component.html',
  styleUrl: './room-list.component.scss'
})
export class RoomListComponent implements OnInit {
  private roomService = inject(RoomService);
  private householdService = inject(HouseholdService);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  householdId: string = '';
  household: any = null;
  rooms: RoomDto[] = [];
  filteredRooms: RoomDto[] = [];
  isOwner = false;
  isSystemAdmin = false;
  canManageRooms = false;
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Filters
  searchQuery = '';
  sortBy: 'name' | 'priority' | 'createdAt' = 'name';
  sortOrder: 'asc' | 'desc' = 'asc';

  // Modal state
  deleteModalRoom: RoomDto | null = null;

ngOnInit(): void {
  this.householdId = this.route.snapshot.queryParamMap.get('householdId') || '';
  
  if (this.householdId) {
    // Check if SystemAdmin
    this.authService.isSystemAdmin$().subscribe(isAdmin => {
      this.isSystemAdmin = isAdmin;
    });

    this.loadHousehold();
    this.loadRooms();
  }
}

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data.household;
          this.isOwner = response.data.isOwner;
          this.canManageRooms = this.isOwner || this.isSystemAdmin;
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to load household';
      }
    });
  }

  loadRooms(): void {
    this.isLoading = true;
    this.error = null;

    this.roomService.getRooms(this.householdId, {
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
      search: this.searchQuery || undefined
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.rooms = response.data;
          this.filteredRooms = response.data;
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load rooms';
        this.isLoading = false;
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.applyFilters();
  }

  onSort(field: 'name' | 'priority' | 'createdAt'): void {
    if (this.sortBy === field) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = field;
      this.sortOrder = 'asc';
    }
    this.loadRooms();
  }

  private applyFilters(): void {
    let filtered = this.rooms;

    if (this.searchQuery) {
      filtered = filtered.filter(r =>
        r.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (r.description && r.description.toLowerCase().includes(this.searchQuery.toLowerCase()))
      );
    }

    this.filteredRooms = filtered;
  }

  openDeleteModal(room: RoomDto): void {
    this.deleteModalRoom = room;
  }

  confirmDelete(): void {
    if (!this.deleteModalRoom) return;

    this.roomService.deleteRoom(this.householdId, this.deleteModalRoom.id).subscribe({
      next: () => {
        this.successMessage = `Room "${this.deleteModalRoom!.name}" deleted successfully`;
        this.deleteModalRoom = null;
        this.loadRooms();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete room';
        this.deleteModalRoom = null;
      }
    });
  }

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }
}