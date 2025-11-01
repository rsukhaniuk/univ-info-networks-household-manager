import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { HouseholdService } from '../services/household.service';
import { HouseholdDetailsDto, HouseholdMemberDto } from '../../../core/models/household.model';
import { UtcDatePipe } from '../../../shared/pipes/utc-date.pipe';

@Component({
  selector: 'app-household-details',
  standalone: true,
  imports: [CommonModule, RouterModule, UtcDatePipe],
  templateUrl: './household-details.component.html',
  styleUrl: './household-details.component.scss'
})
export class HouseholdDetailsComponent implements OnInit {
  private householdService = inject(HouseholdService);
  private route = inject(ActivatedRoute);

  household: HouseholdDetailsDto | null = null;
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Modal state
  showInviteModal = false;
  removeMemberModal: HouseholdMemberDto | null = null;
  inviteCodeCopied = false;

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
        }
        this.isLoading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load household details';
        this.isLoading = false;
      }
    });
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

  private autoHideMessage(): void {
    setTimeout(() => {
      this.successMessage = null;
      this.error = null;
    }, 5000);
  }
}