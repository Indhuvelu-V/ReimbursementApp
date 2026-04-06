import { Component, OnInit } from '@angular/core';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { TokenService } from '../../services/token.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-employee-datas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-datas.html',
  styleUrls: ['./employee-datas.css']
})
export class EmployeeDatas implements OnInit {
  users:        any[] = [];
  allUsers:     any[] = [];
  selectedUser: any   = null;
  searchUserId  = '';
  page          = 1;
  size          = 10;
  totalRecords  = 0;
  role:         string | null = null;

  filterRole = '';
  filterName = '';

  // Assign manager modal
  showAssignModal = false;
  assignTarget:   any = null;
  availableManagers: any[] = [];
  selectedManagerId = '';

  // Status modal
  showStatusModal = false;
  statusTarget:   any = null;
  selectedStatus  = '';
  statusOptions   = ['Active', 'Inactive', 'Suspended'];

  activeTab: 'search' | 'all' = 'search';
  switchTab(tab: 'search' | 'all') {
    this.activeTab = tab;
    if (tab === 'all' && !this.users.length) this.loadUsers();
  }

  constructor(
    private api:          APIService,
    private toast:        ToastService,
    private loader:       LoaderService,
    private tokenService: TokenService
  ) {}

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken();
  }

  isAdmin(): boolean { return this.role === 'Admin'; }

  loadUsers() {
    this.loader.show();
    this.api.getAllUsers(this.page, this.size, this.filterRole || undefined, this.filterName || undefined).subscribe({
      next: (res) => {
        this.users        = res.data ?? res.items ?? [];
        this.totalRecords = res.totalRecords ?? res.totalCount ?? 0;
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load users.'); this.loader.hide(); }
    });
  }

  loadAllUsersForLookup(callback: () => void) {
    this.api.getAllUsers(1, 500).subscribe({
      next: (res) => { this.allUsers = res.data ?? res.items ?? []; callback(); },
      error: () => { this.toast.showError('Failed to load users.'); }
    });
  }

  applyFilters() { this.page = 1; this.loadUsers(); }
  clearFilters() { this.filterRole = ''; this.filterName = ''; this.page = 1; this.loadUsers(); }
  totalPages() { return Math.ceil(this.totalRecords / this.size); }
  nextPage()   { if (this.page < this.totalPages()) { this.page++; this.loadUsers(); } }
  prevPage()   { if (this.page > 1) { this.page--; this.loadUsers(); } }

  searchUser() {
    if (!this.searchUserId.trim()) { this.toast.showWarning('Please enter a User ID.'); return; }
    this.loader.show();
    this.api.getUserById(this.searchUserId).subscribe({
      next: (res) => { this.selectedUser = res; this.toast.show('User found ✅'); this.loader.hide(); },
      error: () => { this.toast.showError('User not found.'); this.selectedUser = null; this.loader.hide(); }
    });
  }

  clearSearch() { this.selectedUser = null; this.searchUserId = ''; }

  // ── Assign Manager ─────────────────────────────────────────────────────────
  openAssignModal(user: any) {
    this.assignTarget = user;
    this.selectedManagerId = user.reportingManagerId ?? '';
    this.loadAllUsersForLookup(() => {
      this.availableManagers = this.allUsers.filter(
        u => u.role === 'Manager' && u.department === user.department
      );
      this.showAssignModal = true;
    });
  }

  confirmAssignManager() {
    if (!this.selectedManagerId) { this.toast.showWarning('Please select a manager.'); return; }
    this.api.assignManager(this.assignTarget.userId, this.selectedManagerId).subscribe({
      next: (res) => {
        this.toast.show(`Manager assigned to ${this.assignTarget.userName} ✅`);
        this.showAssignModal = false;
        const idx = this.users.findIndex(u => u.userId === this.assignTarget.userId);
        if (idx > -1) {
          this.users[idx].reportingManagerId   = res.reportingManagerId;
          this.users[idx].reportingManagerName = res.reportingManagerName;
        }
      },
      error: (err) => this.toast.showError(err?.error?.message || 'Failed to assign manager.')
    });
  }

  // ── Update Status ──────────────────────────────────────────────────────────
  openStatusModal(user: any) {
    this.statusTarget   = user;
    this.selectedStatus = user.status;
    this.showStatusModal = true;
  }

  confirmUpdateStatus() {
    if (!this.selectedStatus) { this.toast.showWarning('Please select a status.'); return; }
    this.api.updateUserStatus(this.statusTarget.userId, this.selectedStatus).subscribe({
      next: (res) => {
        this.toast.show(`Status updated to ${this.selectedStatus} ✅`);
        this.showStatusModal = false;
        const idx = this.users.findIndex(u => u.userId === this.statusTarget.userId);
        if (idx > -1) this.users[idx].status = res.status;
      },
      error: (err) => this.toast.showError(err?.error?.message || 'Failed to update status.')
    });
  }
}
