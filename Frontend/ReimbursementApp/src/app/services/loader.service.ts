import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoaderService {
  private requestCount = 0;
  isLoading = signal(false);

  show() {
    this.requestCount++;
    this.isLoading.set(true);
  }

  hide() {
    this.requestCount--;
    if (this.requestCount <= 0) {
      this.isLoading.set(false);
      this.requestCount = 0;
    }
  }

  /** Force-stop spinner immediately (e.g. after toast fires) */
  forceHide() {
    this.requestCount = 0;
    this.isLoading.set(false);
  }
}
