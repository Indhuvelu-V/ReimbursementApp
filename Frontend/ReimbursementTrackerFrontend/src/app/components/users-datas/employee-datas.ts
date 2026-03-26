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
  selectedUser: any   = null;
  searchUserId  = '';
  page          = 1;
  size          = 10;
  totalRecords  = 0;
  role:         string | null = null;

  // Filters (server-side)
  filterRole = '';
  filterName = '';

  constructor(
    private userService:  APIService,
    private toast:        ToastService,
    private loader:       LoaderService,
    private tokenService: TokenService
  ) {}

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken();
    if (this.isAdmin()) this.loadUsers();
  }

  isAdmin(): boolean { return this.role === 'Admin'; }

  loadUsers() {
    this.loader.show();
    this.userService.getAllUsers(
      this.page, this.size,
      this.filterRole || undefined,
      this.filterName || undefined
    ).subscribe({
      next: (res) => {
        this.users        = res.data ?? res.items ?? [];
        this.totalRecords = res.totalRecords ?? res.totalCount ?? 0;
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load users.');
        this.loader.hide();
      }
    });
  }

  applyFilters() { this.page = 1; this.loadUsers(); }

  clearFilters() {
    this.filterRole = ''; this.filterName = '';
    this.page = 1; this.loadUsers();
  }

  totalPages() { return Math.ceil(this.totalRecords / this.size); }
  nextPage()   { if (this.page < this.totalPages()) { this.page++; this.loadUsers(); } }
  prevPage()   { if (this.page > 1) { this.page--; this.loadUsers(); } }

  searchUser() {
    if (!this.searchUserId.trim()) { this.toast.showWarning('Please enter a User ID.'); return; }
    this.loader.show();
    this.userService.getUserById(this.searchUserId).subscribe({
      next: (res) => { this.selectedUser = res; this.toast.show('User found ✅'); this.loader.hide(); },
      error: () => { this.toast.showError('User not found.'); this.selectedUser = null; this.loader.hide(); }
    });
  }

  clearSearch() { this.selectedUser = null; this.searchUserId = ''; }
}
