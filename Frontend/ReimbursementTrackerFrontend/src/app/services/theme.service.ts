import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {

  private readonly STORAGE_KEY = 'rt_theme';

  /** Reactive signal — components can bind to this */
  mode = signal<ThemeMode>('dark');

  constructor() {
    // Restore saved preference (or detect OS preference)
    const saved = localStorage.getItem(this.STORAGE_KEY) as ThemeMode | null;
    const preferred: ThemeMode = saved
      ?? (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    this.apply(preferred);
  }

  toggle() {
    this.apply(this.mode() === 'dark' ? 'light' : 'dark');
  }

  private apply(mode: ThemeMode) {
    this.mode.set(mode);
    document.documentElement.setAttribute('data-theme', mode);
    localStorage.setItem(this.STORAGE_KEY, mode);
  }

  get isDark(): boolean { return this.mode() === 'dark'; }
  get isLight(): boolean { return this.mode() === 'light'; }
}
