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
  page   = 1;
  size   = 10;
  totalRecords = 0;
  role:  string | null = null;

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
    this.userService.getAllUsers(this.page, this.size).subscribe({
      next: (res) => {
        this.users        = res.data ?? res.items ?? [];
        this.totalRecords = res.totalRecords ?? res.totalCount ?? 0;
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load users. Please try again.');
        this.loader.hide();
      }
    });
  }

  nextPage() {
    if (this.page * this.size < this.totalRecords) { this.page++; this.loadUsers(); }
  }

  prevPage() {
    if (this.page > 1) { this.page--; this.loadUsers(); }
  }

  searchUser() {
    if (!this.searchUserId.trim()) {
      this.toast.showWarning('Please enter a User ID to search.');
      return;
    }
    this.loader.show();
    this.userService.getUserById(this.searchUserId).subscribe({
      next: (res) => {
        this.selectedUser = res;
        this.toast.show('User found ✅');
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('User not found. Please check the User ID.');
        this.selectedUser = null;
        this.loader.hide();
      }
    });
  }

  clearSearch() {
    this.selectedUser = null;
    this.searchUserId = '';
  }
}
