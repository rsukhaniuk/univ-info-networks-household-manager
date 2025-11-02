import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { HouseholdDetailsDto, HouseholdMemberDto } from '../../../core/models/household.model';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';
import { HouseholdContext } from '../services/household-context';

@Component({
  selector: 'app-household-details',
  standalone: true,
  imports: [CommonModule, RouterModule, UtcDatePipe],
  templateUrl: './household-details.component.html',
  styleUrl: './household-details.component.scss'
})
export class HouseholdDetailsComponent implements OnInit, OnDestroy {
  private householdService = inject(HouseholdService);
  private route = inject(ActivatedRoute);
  private householdContext = inject(HouseholdContext);

  household: HouseholdDetailsDto | null = null;
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Modal state
  showInviteModal = false;
  removeMemberModal: HouseholdMemberDto | null = null;
  inviteCodeCopied = false;
  showLeaveHouseholdModal = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadHousehold(id);
    }
  }

  loadHousehold(id: string): void {
    this.isLoading = true;
    this.error = null;

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
      error: (error) => {
        this.error = error.message || 'Failed to load household details';
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy(): void {
    // Clear household context when leaving the page
    this.householdContext.clearHousehold();
  }

  openInviteModal(): void {
    this.showInviteModal = true;
    this.inviteCodeCopied = false;
  }

  closeInviteModal(): void {
    this.showInviteModal = false;
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

  regenerateInviteCode(): void {
    if (!this.household) return;

    const confirmed = confirm('Are you sure you want to generate a new invite code? The old code will no longer work.');
    if (!confirmed) return;

    this.householdService.regenerateInviteCode(this.household.household.id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.household!.household.inviteCode = response.data;
          this.successMessage = 'Invite code regenerated successfully';
          this.autoHideMessage();
        }
      },
      error: (error) => {
        this.error = error.message || 'Failed to regenerate invite code';
      }
    });
  }

  openRemoveMemberModal(member: HouseholdMemberDto): void {
    this.removeMemberModal = member;
  }

  closeRemoveMemberModal(): void {
    this.removeMemberModal = null;
  }

  confirmRemoveMember(): void {
    if (!this.removeMemberModal || !this.household) return;

    this.householdService.removeMember(
      this.household.household.id,
      this.removeMemberModal.userId
    ).subscribe({
      next: () => {
        this.successMessage = `${this.removeMemberModal!.userName} removed successfully`;
        this.removeMemberModal = null;
        this.loadHousehold(this.household!.household.id);
        this.autoHideMessage();
      },
      error: (error) => {
        this.error = error.message || 'Failed to remove member';
        this.removeMemberModal = null;
      }
    });
  }

  openLeaveHouseholdModal(): void {
    this.showLeaveHouseholdModal = true;
  }

  closeLeaveHouseholdModal(): void {
    this.showLeaveHouseholdModal = false;
  }

  confirmLeaveHousehold(): void {
    if (!this.household) return;

    this.householdService.leaveHousehold(this.household.household.id).subscribe({
      next: () => {
        this.successMessage = 'You have left the household successfully';
        this.showLeaveHouseholdModal = false;
        // Redirect to households list after a short delay
        setTimeout(() => {
          window.location.href = '/households';
        }, 1500);
      },
      error: (error) => {
        this.error = error.message || 'Failed to leave household';
        this.showLeaveHouseholdModal = false;
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