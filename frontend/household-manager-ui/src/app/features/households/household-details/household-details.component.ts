import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { HouseholdDetailsDto, HouseholdMemberDto, HouseholdRole } from '../../../core/models/household.model';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';
import { HouseholdContext } from '../services/household-context';
import { ConfirmationDialogComponent, ConfirmDialogData } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-household-details',
  standalone: true,
  imports: [CommonModule, RouterModule, UtcDatePipe, ConfirmationDialogComponent],
  templateUrl: './household-details.component.html',
  styleUrl: './household-details.component.scss'
})
export class HouseholdDetailsComponent implements OnInit, OnDestroy {
  private householdService = inject(HouseholdService);
  private route = inject(ActivatedRoute);
  private householdContext = inject(HouseholdContext);
  private toastService = inject(ToastService);
  private authService = inject(AuthService);

  household: HouseholdDetailsDto | null = null;
  isLoading = true;
  isSystemAdmin$ = this.authService.isSystemAdmin$();

  // Modal state
  showInviteModal = false;
  inviteModalVisible = false;
  inviteCodeCopied = false;

  // Confirmation dialog
  showConfirmDialog = false;
  confirmDialogData: ConfirmDialogData = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmClass: 'danger'
  };
  private pendingAction: (() => void) | null = null;

  // Role management
  HouseholdRole = HouseholdRole;
  availableRoles = [HouseholdRole.Member, HouseholdRole.Owner];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadHousehold(id);
    }
  }

  loadHousehold(id: string): void {
    this.isLoading = true;

    this.householdService.getHouseholdById(id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household = response.data;

          // Set household context for navigation
          this.householdContext.setHousehold({
            id: this.household.household.id,
            name: this.household.household.name,
            isOwner: this.household.isOwner
          });
        }
        this.isLoading = false;
      },
      error: () => {
        // Error will be shown by error interceptor
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy(): void {
    // Don't clear context - allow it to persist when navigating to rooms/tasks
  }

  openInviteModal(): void {
    this.showInviteModal = true;
    this.inviteCodeCopied = false;
    // Trigger animation after modal is rendered
    setTimeout(() => {
      this.inviteModalVisible = true;
    }, 10);
  }

  closeInviteModal(): void {
    this.inviteModalVisible = false;
    // Wait for animation to complete before removing from DOM
    setTimeout(() => {
      this.showInviteModal = false;
    }, 200);
  }

  onInviteModalBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('invite-modal')) {
      this.closeInviteModal();
    }
  }

  showRegenerateConfirm(): void {
    if (!this.household) return;

    this.showConfirmDialog = true;
    this.confirmDialogData = {
      title: 'Generate New Code',
      message: 'Are you sure you want to generate a new invite code? The old code will no longer work.',
      confirmText: 'Generate',
      cancelText: 'Cancel',
      confirmClass: 'warning',
      icon: 'fa-refresh',
      iconClass: 'text-warning'
    };
    this.pendingAction = () => {
      this.performRegenerateInviteCode();
    };
  }

  copyInviteCode(): void {
    if (!this.household) return;

    const inviteCode = this.household.household.inviteCode;
    
    navigator.clipboard.writeText(inviteCode).then(() => {
      this.inviteCodeCopied = true;
      setTimeout(() => {
        this.inviteCodeCopied = false;
      }, 2000);
    }).catch(err => {
      console.error('Failed to copy:', err);
      alert(`Invite code: ${inviteCode}`);
    });
  }

  private performRegenerateInviteCode(): void {
    if (!this.household) return;

    this.householdService.regenerateInviteCode(this.household.household.id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household!.household.inviteCode = response.data.inviteCode;
          this.household!.household.inviteCodeExpiresAt = response.data.inviteCodeExpiresAt;
          this.toastService.success('Invite code regenerated successfully');
        }
      },
      error: () => {
        // Error will be shown by error interceptor
      }
    });
  }

  confirmRemoveMember(member: HouseholdMemberDto): void {
    this.showConfirmDialog = true;
    this.confirmDialogData = {
      title: 'Remove Member',
      message: `Are you sure you want to remove ${member.userName} from this household?\n\nThey will lose access to all rooms, tasks, and data.`,
      confirmText: 'Remove',
      cancelText: 'Cancel',
      confirmClass: 'danger'
    };
    this.pendingAction = () => {
      if (!this.household) return;

      this.householdService.removeMember(
        this.household.household.id,
        member.userId
      ).subscribe({
        next: () => {
          this.toastService.success(`${member.userName} removed successfully`);
          this.loadHousehold(this.household!.household.id);
        },
        error: () => {
          // Error will be shown by error interceptor
        }
      });
    };
  }

  confirmLeaveHousehold(): void {
    if (!this.household) return;

    this.showConfirmDialog = true;
    this.confirmDialogData = {
      title: 'Leave Household',
      message: `Are you sure you want to leave "${this.household.household.name}"?\n\nYou will lose access to all rooms, tasks, and data. You can only rejoin if you receive a new invite.`,
      confirmText: 'Leave',
      cancelText: 'Cancel',
      confirmClass: 'danger',
      icon: 'fa-sign-out-alt',
      iconClass: 'text-danger'
    };
    this.pendingAction = () => {
      if (!this.household) return;

      this.householdService.leaveHousehold(this.household.household.id).subscribe({
        next: () => {
          this.toastService.success(`You have left "${this.household!.household.name}"`);
          // Redirect to households list after a short delay
          setTimeout(() => {
            window.location.href = '/households';
          }, 1500);
        },
        error: () => {
          // Error will be shown by error interceptor
        }
      });
    };
  }

  onDialogConfirmed(): void {
    this.showConfirmDialog = false;
    if (this.pendingAction) {
      this.pendingAction();
      this.pendingAction = null;
    }
  }

  onDialogCancelled(): void {
    this.showConfirmDialog = false;
    this.pendingAction = null;
  }

  /**
   * Update member role (promote/demote)
   */
  changeMemberRole(member: HouseholdMemberDto, newRole: HouseholdRole): void {
    if (!this.household) return;
    if (member.role === newRole) return; // No change

    const roleNames: { [key in HouseholdRole]: string } = {
      [HouseholdRole.Member]: 'Member',
      [HouseholdRole.Owner]: 'Owner'
    };

    this.showConfirmDialog = true;

    // Special handling for ownership transfer
    if (newRole === HouseholdRole.Owner) {
      this.confirmDialogData = {
        title: 'Transfer Ownership',
        message: `Are you sure you want to transfer ownership to ${member.userName}?\n\nIMPORTANT: You will be automatically demoted to Member role and will lose owner privileges.`,
        confirmText: 'Transfer Ownership',
        cancelText: 'Cancel',
        confirmClass: 'warning',
        icon: 'fa-crown',
        iconClass: 'text-warning'
      };
    } else {
      this.confirmDialogData = {
        title: 'Change Member Role',
        message: `Are you sure you want to change ${member.userName}'s role from ${roleNames[member.role]} to ${roleNames[newRole]}?`,
        confirmText: 'Change Role',
        cancelText: 'Cancel',
        confirmClass: 'primary',
        icon: 'fa-user-shield',
        iconClass: 'text-primary'
      };
    }

    this.pendingAction = () => {
      if (!this.household) return;

      this.householdService.updateMemberRole(
        this.household.household.id,
        member.userId,
        { newRole }
      ).subscribe({
        next: () => {
          this.toastService.success(`${member.userName}'s role updated to ${roleNames[newRole]}`);
          this.loadHousehold(this.household!.household.id);
        },
        error: () => {
          // Error will be shown by error interceptor
        }
      });
    };
  }
}