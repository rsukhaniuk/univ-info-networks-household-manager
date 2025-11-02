import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HouseholdContext, HouseholdContextData } from '../../../features/households/services/household-context';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  authService = inject(AuthService);
  householdContext = inject(HouseholdContext);
  router = inject(Router);

  isAuthenticated$ = this.authService.isAuthenticated$;
  user$ = this.authService.user$;
  isSystemAdmin$ = this.authService.isSystemAdmin$();
  currentHousehold$ = this.householdContext.currentHousehold$;

  login(): void {
    this.authService.login();
  }

  signup(): void {
    this.authService.signup();
  }

  logout(): void {
    this.authService.logout();
  }

  isActive(path: string): boolean {
    return this.router.url.includes(path);
  }
}