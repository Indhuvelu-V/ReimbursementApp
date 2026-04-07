import { Component, OnInit } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationBell } from '../components/notification-bell/notification-bell';
import { TokenService } from '../services/token.service';
import { ThemeService } from '../services/theme.service';
import { APIService } from '../services/api.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterModule, CommonModule, NotificationBell, FormsModule],
  templateUrl: './employee-dashboard.html',
  styleUrls: ['./employee-dashboard.css']
})
export class EmployeeDashboard implements OnInit {
  navOpen = false;
  showLogoutConfirm = false;
  showProfileMenu = false;   // small dropdown with two options
  showViewModal   = false;   // view profile modal
  showEditModal   = false;   // update details modal

  userProfile: any = null;
  editForm = { phone: '', bankName: '', accountNumber: '', ifscCode: '', branchName: '' };
  editErrors: any = {};

  constructor(
    private tokenService: TokenService,
    private router: Router,
    public themeService: ThemeService,
    private api: APIService,
    private toast: ToastService
  ) {}

  ngOnInit() {
    const userId = this.tokenService.getUserIdFromToken();
    if (userId) {
      this.api.getUserById(userId).subscribe({
        next: (res: any) => { this.userProfile = res; },
        error: () => {}
      });
    }
  }

  uname(): string | null { return this.tokenService.getUsernameFromToken(); }
  toggleNav()   { this.navOpen = !this.navOpen; }
  toggleTheme() { this.themeService.toggle(); }

  toggleProfileMenu() { this.showProfileMenu = !this.showProfileMenu; }

  openViewProfile() {
    this.showProfileMenu = false;
    this.showViewModal = true;
  }

  openEditProfile() {
    this.showProfileMenu = false;
    this.editForm = {
      phone:         this.userProfile?.phone         ?? '',
      bankName:      this.userProfile?.bankName      ?? '',
      accountNumber: this.userProfile?.accountNumber ?? '',
      ifscCode:      this.userProfile?.ifscCode      ?? '',
      branchName:    this.userProfile?.branchName    ?? ''
    };
    this.showEditModal = true;
  }

  saveProfile() {
    this.editErrors = {};
    // Validate phone if provided
    if (this.editForm.phone && !/^(?:\+91|0)?[6-9]\d{9}$/.test(this.editForm.phone)) {
      this.editErrors.phone = 'Enter a valid 10-digit mobile number.';
    }
    // Validate account number if provided
    if (this.editForm.accountNumber && !/^\d{9,18}$/.test(this.editForm.accountNumber)) {
      this.editErrors.accountNumber = 'Account number must be 9–18 digits.';
    }
    // Validate IFSC if provided
    if (this.editForm.ifscCode && !/^[A-Z]{4}0[A-Z0-9]{6}$/i.test(this.editForm.ifscCode)) {
      this.editErrors.ifscCode = 'Invalid IFSC code format (e.g. UTIB0001234).';
    }
    if (Object.keys(this.editErrors).length > 0) return;

    // Send existing value if field is empty (preserve old data)
    this.api.updateMyProfile({
      phone:         this.editForm.phone         || this.userProfile?.phone         || undefined,
      bankName:      this.editForm.bankName      || this.userProfile?.bankName      || undefined,
      accountNumber: this.editForm.accountNumber || this.userProfile?.accountNumber || undefined,
      ifscCode:      (this.editForm.ifscCode     || this.userProfile?.ifscCode      || '')?.toUpperCase() || undefined,
      branchName:    this.editForm.branchName    || this.userProfile?.branchName    || undefined
    }).subscribe({
      next: (res: any) => {
        this.userProfile = res;
        this.showEditModal = false;
        this.editErrors = {};
        this.toast.show('Profile updated successfully ✅');
      },
      error: (err: any) => this.toast.showError(err?.error?.message || 'Failed to update profile.')
    });
  }

  logout(event: Event) {
    event.preventDefault();
    this.showLogoutConfirm = true;
  }

  confirmLogout() {
    this.showLogoutConfirm = false;
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }

  cancelLogout() { this.showLogoutConfirm = false; }
}
