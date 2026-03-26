import { Component } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NotificationBell } from '../components/notification-bell/notification-bell';
import { TokenService } from '../services/token.service';
import { ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell],
  templateUrl: './employee-dashboard.html',
  styleUrls: ['./employee-dashboard.css']
})
export class EmployeeDashboard {
  navOpen = false;
  showLogoutConfirm = false;

  constructor(
    private tokenService: TokenService,
    private router: Router,
    public themeService: ThemeService
  ) {}

  uname(): string | null { return this.tokenService.getUsernameFromToken(); }
  toggleNav()   { this.navOpen = !this.navOpen; }
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
