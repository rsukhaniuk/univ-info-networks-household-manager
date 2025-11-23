import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TableLazyLoadEvent } from 'primeng/table';
import { Subject, debounceTime, finalize } from 'rxjs';

import { RoomService } from '../services/room.service';
import { HouseholdService } from '../../households/services/household.service';
import { HouseholdContext } from '../../households/services/household-context';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingService } from '../../../core/services/loading.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoomDto } from '../../../core/models/room.model';
import { HouseholdDto } from '../../../core/models/household.model';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';
import { PhotoUrlPipe } from '../../../shared/pipes/photo-url.pipe';

type SortBy = 'name' | 'priority' | 'createdAt';
type SortOrder = 'asc' | 'desc';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TableModule,
    InputTextModule,
    ButtonModule,
    SkeletonModule,
    ConfirmationDialogComponent,
    UtcDatePipe,
    PhotoUrlPipe
  ],
  templateUrl: './room-list.component.html',
  styleUrl: './room-list.component.scss'
})
export class RoomListComponent implements OnInit, OnDestroy {
  private roomService = inject(RoomService);
  private householdService = inject(HouseholdService);
  private householdContext = inject(HouseholdContext);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  private loadingService = inject(LoadingService);
  private toastService = inject(ToastService);

  isSystemAdmin$ = this.auth.isSystemAdmin$();

  householdId: string = '';
  household: HouseholdDto | null = null;
  isOwner = false;
  canManageRooms = false;

  loading = true;
  private loadingDelayMs = 400;
  private loadingTimer: any = null;
  rows = 10;
  first = 0;
  totalRecords = 0;

  items: RoomDto[] = [];

  searchQuery = '';
  private search$ = new Subject<string>();
  private searchSubscription: any;

  currentSortField: SortBy = 'name';
  currentSortOrder: SortOrder = 'asc';

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
    this.householdId = this.route.snapshot.queryParamMap.get('householdId') || '';

    if (this.householdId) {
      this.loadHousehold();
    }

    this.searchSubscription = this.search$
      .pipe(debounceTime(300))
      .subscribe(q => {
        this.searchQuery = q;
        this.first = 0;
        this.loadData();
      });
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
    }
    // Clear household context when leaving
    this.householdContext.clearHousehold();
  }

  private loadHousehold(): void {
    this.householdService.getHouseholdById(this.householdId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data.household;
          this.isOwner = response.data.isOwner;
          this.updateCanManageRooms();

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: this.household.id,
            name: this.household.name,
            isOwner: this.isOwner
          });
        }
      },
      error: (error) => {
        // Error will be shown by error interceptor
      }
    });
  }

  private updateCanManageRooms(): void {
    this.auth.isSystemAdmin$().subscribe(isAdmin => {
      this.canManageRooms = this.isOwner || isAdmin;
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.rows;
    const page = Math.floor((event.first ?? 0) / pageSize) + 1;

    const sortField = (event.sortField as SortBy) ?? 'name';
    const sortOrder: SortOrder = (event.sortOrder ?? 1) === 1 ? 'asc' : 'desc';

    this.currentSortField = sortField;
    this.currentSortOrder = sortOrder;

    this.rows = pageSize;
    this.first = (page - 1) * pageSize;

    this.loadData(page, pageSize, sortField, sortOrder);
  }

  private loadData(
    page: number = 1,
    pageSize: number = this.rows,
    sortBy: SortBy = this.currentSortField,
    sortOrder: SortOrder = this.currentSortOrder
  ): void {
    if (this.showConfirmDialog) {
      return;
    }

    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loadingTimer = setTimeout(() => {
      this.loading = true;
      this.loadingTimer = null;
    }, this.loadingDelayMs);

    this.roomService
      .getRooms(this.householdId, {
        page,
        pageSize,
        sortBy,
        sortOrder,
        search: this.searchQuery || undefined,
      })
      .pipe(
        finalize(() => {
          if (this.loadingTimer) {
            clearTimeout(this.loadingTimer);
            this.loadingTimer = null;
          }
          this.loading = false;
        })
      )
      .subscribe({
        next: (res) => {
          this.items = res.data?.items ?? [];
          this.totalRecords = res.data?.totalCount ?? 0;
        },
        error: (err) => {
          // Error will be shown by error interceptor
          this.items = [];
          this.totalRecords = 0;
        }
      });
  }

  onSearch(query: string): void {
    this.search$.next(query);
  }

  onPageSizeChange(size: number): void {
    this.rows = size;
    this.first = 0;
    this.loadData(1, size);
  }

  reload(): void {
    const currentPage = Math.floor(this.first / this.rows) + 1;
    this.loadData(currentPage, this.rows);
  }

  confirmDelete(room: RoomDto): void {
    this.loadingService.beginSuppress();

    this.showConfirmDialog = true;

    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loading = false;

    this.confirmDialogData = {
      title: 'Delete Room',
      message: `Are you sure you want to delete "${room.name}"?\n\nThis action cannot be undone. All tasks and execution history in this room will be permanently deleted.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-trash-alt',
      iconClass: 'text-danger'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;

      this.loadingService.endSuppress();

      this.roomService.deleteRoom(this.householdId, room.id).subscribe({
        next: () => {
          this.toastService.success(`Room "${room.name}" deleted successfully`);
          setTimeout(() => this.reload(), 150);
        },
        error: (err) => {
          // Error will be shown by error interceptor
        }
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

    this.loadingService.endSuppress();
  }
}