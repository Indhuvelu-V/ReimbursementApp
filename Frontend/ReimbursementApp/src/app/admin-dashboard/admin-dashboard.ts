// import { Component } from '@angular/core';
// import { Router, RouterModule, RouterOutlet } from '@angular/router';
// import { CommonModule } from '@angular/common';
// import { NotificationBell } from '../components/notification-bell/notification-bell';
// import { TokenService } from '../services/token.service';

// @Component({
//   selector: 'app-admin-dashboard',
//   standalone: true,
//   imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
//   templateUrl: './admin-dashboard.html',
//   styleUrls: ['./admin-dashboard.css']
// })
// export class AdminDashboard {
//   constructor(private tokenService: TokenService, private router: Router) {}
//   uname(): string | null { return this.tokenService.getUsernameFromToken(); }
//   logout(event: Event) {
//     event.preventDefault();
//     sessionStorage.removeItem('token');
//     this.router.navigate(['/login']);
//   }
// }
// src/app/admin-dashboard/admin-dashboard.ts — FULL FILE

import { Component } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NotificationBell } from '../components/notification-bell/notification-bell';
import { TokenService } from '../services/token.service';
import { ThemeService } from '../services/theme.service';


@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboard {

  // Local frontend state — controls logout confirmation modal
  showLogoutConfirm = false;

  constructor(
    private tokenService: TokenService,
    private router: Router,
    private themeService: ThemeService
  ) {}

  uname(): string | null { return this.tokenService.getUsernameFromToken(); }

  // Theme
  isDark()      { return this.themeService.isDark(); }
  toggleTheme() { this.themeService.toggle(); }

  // Step 1: intercept click, show modal
  confirmLogout(event: Event) {
    event.preventDefault();
    this.showLogoutConfirm = true;
  }

  // Step 2a: user clicked Cancel
  cancelLogout() {
    this.showLogoutConfirm = false;
  }

  // Step 2b: user clicked Logout → run original logout
  confirmLogoutAction(event: Event) {
    this.showLogoutConfirm = false;
    this.logout(event);
  }

  // Original logout logic — DO NOT CHANGE
  logout(event: Event) {
    event.preventDefault();
    sessionStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
