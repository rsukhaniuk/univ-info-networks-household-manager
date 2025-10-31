import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private requestCount = 0;
  loading$ = this.loadingSubject.asObservable();

  private showDelayMs = 500; 
  private minVisibleMs = 400; 
  private showTimer: any = null;
  private hideTimer: any = null;
  private visible = false;
  private visibleSince: number | null = null;
  private suppressCount = 0;

  beginSuppress(): void {
    this.suppressCount++;
    if (this.showTimer) {
      clearTimeout(this.showTimer);
      this.showTimer = null;
    }
    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }
    if (this.visible) {
      this.visible = false;
      this.visibleSince = null;
      this.loadingSubject.next(false);
    }
  }

  endSuppress(): void {
    this.suppressCount = Math.max(0, this.suppressCount - 1);
    if (this.suppressCount === 0 && this.requestCount > 0 && !this.visible && !this.showTimer) {
      this.scheduleShow();
    }
  }

  private isSuppressed(): boolean {
    return this.suppressCount > 0;
  }

  private scheduleShow(): void {
    if (this.isSuppressed() || this.visible || this.showTimer) {
      return;
    }
    this.showTimer = setTimeout(() => {
      this.showTimer = null;
      if (this.requestCount > 0 && !this.isSuppressed()) {
        this.visible = true;
        this.visibleSince = Date.now();
        this.loadingSubject.next(true);
      }
    }, this.showDelayMs);
  }

  show(): void {
    this.requestCount++;

    if (this.isSuppressed()) {
      return;
    }

    if (this.visible) {
      return;
    }

    if (this.showTimer) {
      return;
    }

    this.scheduleShow();
  }

  hide(): void {
    this.requestCount--;
    if (this.requestCount <= 0) {
      this.requestCount = 0;

      if (this.showTimer) {
        clearTimeout(this.showTimer);
        this.showTimer = null;
      }

      if (this.visible) {
        const elapsed = this.visibleSince ? Date.now() - this.visibleSince : this.minVisibleMs;
        const remaining = Math.max(this.minVisibleMs - elapsed, 0);

        if (this.hideTimer) {
          clearTimeout(this.hideTimer);
          this.hideTimer = null;
        }

        if (remaining > 0) {
          this.hideTimer = setTimeout(() => {
            this.hideTimer = null;
            this.visible = false;
            this.visibleSince = null;
            this.loadingSubject.next(false);
          }, remaining);
        } else {
          this.visible = false;
          this.visibleSince = null;
          this.loadingSubject.next(false);
        }
      }
    }
  }
}