import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { AuthService } from '../../../core/services/auth.service';
import { HouseholdDto } from '../../../core/models/household.model';

@Component({
  selector: 'app-household-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './household-list.component.html',
  styleUrl: './household-list.component.scss'
})
export class HouseholdListComponent implements OnInit {
  private householdService = inject(HouseholdService);
  private authService = inject(AuthService);

  households: HouseholdDto[] = [];
  filteredHouseholds: HouseholdDto[] = [];
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  isSystemAdmin$ = this.authService.isSystemAdmin$();

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchQuery = '';
  sortBy: 'name' | 'createdAt' | 'memberCount' = 'createdAt';
  sortOrder: 'asc' | 'desc' = 'desc';

  // Modal state
  deleteModalHousehold: HouseholdDto | null = null;
  leaveModalHousehold: HouseholdDto | null = null;

  ngOnInit(): void {
    this.loadHouseholds();
  }

  loadHouseholds(): void {
    this.isLoading = true;
    this.error = null;

    this.householdService.getHouseholds({
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
      search: this.searchQuery || undefined
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.households = response.data.items;
          this.filteredHouseholds = response.data.items;
          this.totalCount = response.data.totalCount;
          this.totalPages = response.data.totalPages;
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load households';
        this.isLoading = false;
      }
    });
  }

  onSearch(query: string): void {
    this.searchQuery = query;
    this.currentPage = 1;
    this.loadHouseholds();
  }

  onSort(field: 'name' | 'createdAt' | 'memberCount'): void {
    if (this.sortBy === field) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = field;
      this.sortOrder = 'desc';
    }
    this.loadHouseholds();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadHouseholds();
  }

  openDeleteModal(household: HouseholdDto): void {
    this.deleteModalHousehold = household;
  }

  openLeaveModal(household: HouseholdDto): void {
    this.leaveModalHousehold = household;
  }

  confirmDelete(): void {
    if (!this.deleteModalHousehold) return;

    this.householdService.deleteHousehold(this.deleteModalHousehold.id).subscribe({
      next: () => {
        this.successMessage = `Household "${this.deleteModalHousehold!.name}" deleted successfully`;
        this.deleteModalHousehold = null;
        this.loadHouseholds();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to delete household';
        this.deleteModalHousehold = null;
      }
    });
  }

  confirmLeave(): void {
    if (!this.leaveModalHousehold) return;

    this.householdService.leaveHousehold(this.leaveModalHousehold.id).subscribe({
      next: () => {
        this.successMessage = `You have left "${this.leaveModalHousehold!.name}"`;
        this.leaveModalHousehold = null;
        this.loadHouseholds();
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to leave household';
        this.leaveModalHousehold = null;
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