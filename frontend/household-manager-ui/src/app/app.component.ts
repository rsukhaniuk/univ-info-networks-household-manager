import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { ServerErrorService } from './core/services/server-error.service';
import { LoadingService } from './core/services/loading.service';
import { ToastService } from './core/services/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, HeaderComponent, FooterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  title = 'Household Manager';

  private serverErrorService = inject(ServerErrorService);
  private loadingService = inject(LoadingService);
  private toastService = inject(ToastService);

  errors$ = this.serverErrorService.errors$;
  loading$ = this.loadingService.loading$;
  toasts$ = this.toastService.toasts$;

  ngOnInit(): void {
    // Auto-redirect from 127.0.0.1 to localhost for consistency
    if (window.location.hostname === '127.0.0.1') {
      const newUrl = window.location.href.replace('127.0.0.1', 'localhost');
      window.location.replace(newUrl);
    }
  }

  clearErrors(): void {
    this.serverErrorService.clear();
  }

  removeToast(id: number): void {
    this.toastService.remove(id);
  }
}
