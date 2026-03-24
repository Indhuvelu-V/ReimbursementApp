// import { Component } from '@angular/core';
// import { Router, RouterModule, RouterOutlet } from '@angular/router';
// import { TokenService } from '../services/token.service';
// import { CommonModule } from '@angular/common';
// import { NotificationBell } from '../components/notification-bell/notification-bell';

// @Component({
//   selector: 'app-manager-dashboard',
//   standalone: true,
//   imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
//   templateUrl: './manager-dashboard.html',
//   styleUrls: ['./manager-dashboard.css']
// })
// export class ManagerDashboard {
//   constructor(private tokenService: TokenService, private router: Router) {}
//   uname(): string | null { return this.tokenService.getUsernameFromToken(); }
//   logout(event: Event) {
//     event.preventDefault();
//     sessionStorage.removeItem('token');
//     this.router.navigate(['/login']);
//   }
//   logout(event: Event) {
//   event.preventDefault();

//   const confirmLogout = confirm('Are you sure you want to logout?');

//   if (confirmLogout) {
//     sessionStorage.removeItem('token');
//     this.router.navigate(['/login']);
//   }
// }
// }
// import { Component } from '@angular/core';
// import { Router, RouterModule, RouterOutlet } from '@angular/router';
// import { TokenService } from '../services/token.service';
// import { ToastService } from '../services/toast.service';
// import { CommonModule } from '@angular/common';
// import { NotificationBell } from '../components/notification-bell/notification-bell';

// @Component({
//   selector: 'app-manager-dashboard',
//   standalone: true,
//   imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
//   templateUrl: './manager-dashboard.html',
//   styleUrls: ['./manager-dashboard.css']
// })
// export class ManagerDashboard {

//   private logoutConfirm = false;

//   constructor(
//     private tokenService: TokenService,
//     private router: Router,
//     private toastService: ToastService
//   ) {}

//   uname(): string | null {
//     return this.tokenService.getUsernameFromToken();
//   }

//   logout(event: Event) {
//     event.preventDefault();


//     if (this.logoutConfirm) {
//       sessionStorage.removeItem('token');
//       this.toastService.show('Logged out successfully', 'success');
//       this.router.navigate(['/login']);
//       this.logoutConfirm = false;
//       return;
//     }

  
//     this.logoutConfirm = true;
//     this.toastService.showWarning('Click logout again to confirm');

   
//     setTimeout(() => {
//       this.logoutConfirm = false;
//     }, 3000);
//   }
// }
// src/app/manager-dashboard/manager-dashboard.ts — FULL FILE

import { Component } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { TokenService } from '../services/token.service';
import { CommonModule } from '@angular/common';
import { NotificationBell } from '../components/notification-bell/notification-bell';
import { ThemeService } from '../services/theme.service';


@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
  templateUrl: './manager-dashboard.html',
  styleUrls: ['./manager-dashboard.css']
})
export class ManagerDashboard {

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
