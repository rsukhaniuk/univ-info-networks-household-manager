import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  authService = inject(AuthService);
  router = inject(Router);

  isAuthenticated$ = this.authService.isAuthenticated$;
  isLoading$ = this.authService.isLoading$;
  user$ = this.authService.user$;

  login(): void {
    this.authService.login('/households');
  }

  signup(): void {
    this.authService.signup();
  }

  goToHouseholds(): void {
    this.router.navigate(['/households']);
  }
}