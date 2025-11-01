import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TableLazyLoadEvent } from 'primeng/table';
import { Subject, debounceTime, finalize } from 'rxjs';

import { HouseholdService } from '../services/household.service';
import { AuthService } from '../../../core/services/auth.service';
import { ServerErrorService } from '../../../core/services/server-error.service';
import { LoadingService } from '../../../core/services/loading.service';
import { ToastService } from '../../../core/services/toast.service';
import { HouseholdDto, HouseholdRole } from '../../../core/models/household.model';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

type SortBy = 'name' | 'createdAt' | 'memberCount';
type SortOrder = 'asc' | 'desc';

@Component({
  selector: 'app-household-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TableModule,
    InputTextModule,
    ButtonModule,
    SkeletonModule,
    ConfirmationDialogComponent,
    UtcDatePipe
  ],
  templateUrl: './household-list.component.html',
  styleUrls: ['./household-list.component.scss'],
})
export class HouseholdListComponent implements OnInit, OnDestroy {
  private service = inject(HouseholdService);
  private auth = inject(AuthService);
  private errors = inject(ServerErrorService);
  private loadingService = inject(LoadingService);
  private toastService = inject(ToastService);

  isSystemAdmin$ = this.auth.isSystemAdmin$();
  errors$ = this.errors.errors$;

  loading = true;
  private loadingDelayMs = 400; // Збільшено затримку, щоб уникнути мерехтіння
  private loadingTimer: any = null;
  rows = 10;
  first = 0;
  totalRecords = 0;

  items: HouseholdDto[] = [];

  searchQuery = '';
  private search$ = new Subject<string>();
  private searchSubscription: any;

  currentSortField: SortBy = 'createdAt';
  currentSortOrder: SortOrder = 'desc';

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
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.rows;
    const page = Math.floor((event.first ?? 0) / pageSize) + 1;
    
    const sortField = (event.sortField as SortBy) ?? 'createdAt';
    const sortOrder: SortOrder = (event.sortOrder ?? -1) === 1 ? 'asc' : 'desc';

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

    this.service
      .getHouseholds({
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
          console.error('Failed to load households:', err);
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

  canEdit(household: HouseholdDto): boolean {
    return false;
  }

  canDelete(household: HouseholdDto): boolean {
    return false;
  }

  canLeave(household: HouseholdDto): boolean {
    return true;
  }

  confirmDelete(household: HouseholdDto): void {
    this.loadingService.beginSuppress();
    
    this.showConfirmDialog = true;
    
    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loading = false;

    this.confirmDialogData = {
      title: 'Delete Household',
      message: `Are you sure you want to delete "${household.name}"?\n\nThis action cannot be undone. All rooms, tasks, and execution history will be permanently deleted.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-trash-alt',
      iconClass: 'text-danger'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;
      
      this.loadingService.endSuppress();

      this.service.deleteHousehold(household.id).subscribe({
        next: () => {
          this.toastService.success(`Household "${household.name}" deleted successfully`);
          setTimeout(() => this.reload(), 150);
        },
        error: (err) => {
          console.error('Failed to delete household:', err);
        }
      });
    };
  }

  confirmLeave(household: HouseholdDto): void {
    this.loadingService.beginSuppress();
    
    this.showConfirmDialog = true;
    
    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
    this.loading = false;

    this.confirmDialogData = {
      title: 'Leave Household',
      message: `Are you sure you want to leave "${household.name}"?\n\nYou will no longer have access to this household's rooms, tasks, and data.`,
      confirmText: 'Leave',
      cancelText: 'Cancel',
      confirmClass: 'warning',
      icon: 'fa-sign-out-alt',
      iconClass: 'text-warning'
    };

    this.pendingAction = () => {
      this.showConfirmDialog = false;
      
      this.loadingService.endSuppress();

      this.service.leaveHousehold(household.id).subscribe({
        next: () => {
          this.toastService.success(`You have left "${household.name}"`);
          setTimeout(() => this.reload(), 150);
        },
        error: (err) => {
          console.error('Failed to leave household:', err);
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

  getRoleBadgeClass(role?: HouseholdRole): string {
    switch (role) {
      case HouseholdRole.Member:
        return 'bg-success';
      case HouseholdRole.Owner:
        return 'bg-primary';
      default:
        return 'bg-secondary';
    }
  }

  getRoleIcon(role?: HouseholdRole): string {
    switch (role) {
      case HouseholdRole.Member:
        return 'fa-user';
      case HouseholdRole.Owner:
        return 'fa-crown';
      default:
        return 'fa-user';
    }
  }
}