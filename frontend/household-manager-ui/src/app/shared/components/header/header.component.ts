import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  authService = inject(AuthService);

  isAuthenticated$ = this.authService.isAuthenticated$;
  user$ = this.authService.user$;
  isSystemAdmin$ = this.authService.isSystemAdmin$();

  login(): void {
    this.authService.login();
  }

  signup(): void {
    this.authService.signup();
  }

  logout(): void {
    this.authService.logout();
  }
}