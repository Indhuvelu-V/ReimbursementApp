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
  navOpen = false;
  showLogoutConfirm = false;

  constructor(
    private tokenService: TokenService,
    private router: Router,
    public themeService: ThemeService
  ) {}

  uname(): string | null { return this.tokenService.getUsernameFromToken(); }

  toggleNav() { this.navOpen = !this.navOpen; }

  toggleTheme() { this.themeService.toggle(); }

  logout(event: Event) {
    event.preventDefault();
    this.showLogoutConfirm = true;
  }

  confirmLogout() {
    this.showLogoutConfirm = false;
    sessionStorage.removeItem('token');
    this.router.navigate(['/login']);
  }

  cancelLogout() { this.showLogoutConfirm = false; }
}
