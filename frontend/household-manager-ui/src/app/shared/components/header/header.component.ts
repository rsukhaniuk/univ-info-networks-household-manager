import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { HouseholdContext, HouseholdContextData } from '../../../features/households/services/household-context';
import { UserService } from '../../../features/profile/services/user.service';
import { Observable, map, of, switchMap, filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  authService = inject(AuthService);
  userService = inject(UserService);
  householdContext = inject(HouseholdContext);
  router = inject(Router);

  isAuthenticated$ = this.authService.isAuthenticated$;
  user$ = this.authService.user$;
  isSystemAdmin$ = this.authService.isSystemAdmin$();
  currentHousehold$ = this.householdContext.currentHousehold$;

  // User display name from database (fallback to Auth0)
  userDisplayName$!: Observable<string>;
  private routerSubscription?: Subscription;

  ngOnInit(): void {
    // Clear household context when navigating away from household area
    this.routerSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      const url = event.urlAfterRedirects;

      // Clear context if navigating to non-household pages
      const isHouseholdArea = url.includes('/households/') ||
                            url.includes('/rooms') ||
                            url.includes('/tasks');

      if (!isHouseholdArea && this.householdContext.getCurrentHousehold()) {
        this.householdContext.clearHousehold();
      }
    });
    // Get display name from database, fallback to Auth0 name, then email
    this.userDisplayName$ = this.isAuthenticated$.pipe(
      switchMap(isAuthenticated => {
        if (!isAuthenticated) {
          return of('');
        }

        return this.userService.getMyProfile().pipe(
          map(response => {
            const profile = response.data;
            const fullName = profile?.user?.fullName?.trim();

            // Priority: database fullName > Auth0 name > email
            if (fullName && fullName.length > 0) {
              return fullName;
            }

            // Fallback to Auth0 user observable
            return '';
          }),
          switchMap(dbName => {
            if (dbName) {
              return of(dbName);
            }

            // Fallback to Auth0 user
            return this.user$.pipe(
              map(auth0User => auth0User?.name || auth0User?.email || 'User')
            );
          })
        );
      })
    );
  }

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

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }
}