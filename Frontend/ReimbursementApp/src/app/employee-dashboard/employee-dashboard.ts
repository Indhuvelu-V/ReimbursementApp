// import { Component } from '@angular/core';
// import { Router, RouterModule, RouterOutlet } from '@angular/router';
// import { TokenService } from '../services/token.service';
// import { CommonModule } from '@angular/common';
// import { NotificationBell } from '../components/notification-bell/notification-bell';

// @Component({
//   selector: 'app-employee-dashboard',
//   standalone: true,
//   imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
//   templateUrl: './employee-dashboard.html',
//   styleUrls: ['./employee-dashboard.css']
// })
// export class EmployeeDashboard {
//   constructor(private tokenService: TokenService, private router: Router) {}

//   uname(): string | null { return this.tokenService.getUsernameFromToken(); }

//   logout(event: Event) {
//     event.preventDefault();
//     sessionStorage.removeItem('token');
//     this.router.navigate(['/login']);
//   }
// }
// src/app/employee-dashboard/employee-dashboard.ts — FULL FILE

import { Component } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { TokenService } from '../services/token.service';
import { CommonModule } from '@angular/common';
import { NotificationBell } from '../components/notification-bell/notification-bell';
import { ThemeService } from '../services/theme.service';


@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
  templateUrl: './employee-dashboard.html',
  styleUrls: ['./employee-dashboard.css']
})
export class EmployeeDashboard {

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

  // Logout confirmation
  confirmLogout(event: Event) { event.preventDefault(); this.showLogoutConfirm = true; }
  cancelLogout()              { this.showLogoutConfirm = false; }
  confirmLogoutAction(event: Event) { this.showLogoutConfirm = false; this.logout(event); }

  // Original logout logic — DO NOT CHANGE
  logout(event: Event) {
    event.preventDefault();
    sessionStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}
