import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { ServerErrorService } from './core/services/server-error.service';
import { LoadingService } from './core/services/loading.service';
import { ToastService } from './core/services/toast.service';
import { filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, HeaderComponent, FooterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Household Manager';

  private serverErrorService = inject(ServerErrorService);
  private loadingService = inject(LoadingService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  errors$ = this.serverErrorService.errors$;
  loading$ = this.loadingService.loading$;
  toasts$ = this.toastService.toasts$;

  private routerSubscription?: Subscription;

  ngOnInit(): void {
    // Auto-redirect from 127.0.0.1 to localhost for consistency
    if (window.location.hostname === '127.0.0.1') {
      const newUrl = window.location.href.replace('127.0.0.1', 'localhost');
      window.location.replace(newUrl);
    }

    // Clear global errors on navigation (except callback page)
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        // Don't clear errors when navigating to/from callback page
        // This allows errors from auth flows to be displayed
        if (!event.url.includes('/callback')) {
          this.serverErrorService.clear();
        }
      });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  clearErrors(): void {
    this.serverErrorService.clear();
  }

  removeToast(id: number): void {
    this.toastService.remove(id);
  }
}
