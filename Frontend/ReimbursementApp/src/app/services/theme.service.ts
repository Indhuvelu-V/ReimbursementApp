// src/app/services/theme.service.ts — FULL FILE

import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {

  // Local frontend state — tracks current theme
  isDark = signal<boolean>(false);

  constructor() {
    // Restore saved preference on app load, fallback to system preference
    const saved       = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const startDark   = saved ? saved === 'dark' : prefersDark;
    this.applyTheme(startDark);
  }

  toggle() {
    this.applyTheme(!this.isDark());
  }

  private applyTheme(dark: boolean) {
    this.isDark.set(dark);
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    localStorage.setItem('theme', dark ? 'dark' : 'light');
  }
}
