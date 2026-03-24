import { Component, OnInit } from '@angular/core';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { ToastService } from '../../services/toast.service';
import { TokenService } from '../../services/token.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PagedResponse } from '../../models/approval.model';
import { CreateAuditLogsResponseDto } from '../../models/log.model';

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './logs.html',
  styleUrls: ['./logs.css']
})
export class Logs implements OnInit {

  logs:  any[] = [];
  role:  string | null = null;
  page   = 1;
  size   = 10;
  total  = 0;

  // Delete confirmation popup state — UNCHANGED
  showDeleteConfirm = false;
  pendingDeleteId:  string | null = null;
  pendingDeleteLog: any = null;

  // NEW: date filter state
  fromDate: string = '';
  toDate:   string = '';

  constructor(
    private api:          APIService,
    private loader:       LoaderService,
    private toast:        ToastService,
    private tokenService: TokenService
  ) {}

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken() ?? null;
    if (this.isAdmin()) this.loadLogs();
  }

  isAdmin():     boolean { return this.role?.toLowerCase() === 'admin'; }
  isOtherUser(): boolean { return !this.isAdmin(); }

  // MODIFIED: passes fromDate/toDate as query params
  loadLogs() {
    this.loader.show();
    this.api.getPagedLogs(this.page, this.size, this.fromDate || undefined, this.toDate || undefined)
      .subscribe({
        next: (res: PagedResponse<CreateAuditLogsResponseDto>) => {
          this.logs = res.data ?? [];
          this.total = res.totalRecords ?? this.logs.length;
          this.loader.hide();
        },
        error: () => {
          this.toast.showError('Failed to load audit logs.');
          this.loader.hide();
        }
      });
  }

  applyFilter() {
    this.page = 1;
    this.loadLogs();
  }

  clearFilter() {
    this.fromDate = '';
    this.toDate = '';
    this.page = 1;
    this.loadLogs();
  }

  confirmDelete(log: any) {
    this.pendingDeleteId = log.logId;
    this.pendingDeleteLog = log;
    this.showDeleteConfirm = true;
  }

  cancelDelete() {
    this.showDeleteConfirm = false;
    this.pendingDeleteId = null;
    this.pendingDeleteLog = null;
  }

  executeDelete() {
    if (!this.pendingDeleteId) return;
    const id = this.pendingDeleteId;
    this.cancelDelete();

    this.loader.show();
    this.api.deleteLog(id).subscribe({
      next: () => {
        this.toast.show('Audit log deleted ✅');
        if (this.logs.length === 1 && this.page > 1) this.page--;
        this.loadLogs();
        this.loader.hide();
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Failed to delete log.');
        this.loader.hide();
      }
    });
  }

  nextPage() {
    if (this.page * this.size < this.total) { this.page++; this.loadLogs(); }
  }

  prevPage() {
    if (this.page > 1) { this.page--; this.loadLogs(); }
  }

  totalPages(): number { return Math.ceil(this.total / this.size); }

  formatAmount(amount: any): string {
    if (amount == null || amount === '') return '—';
    const num = Number(amount);
    if (isNaN(num)) return '—';
    return '₹' + num.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatDate(dateVal: any): string {
    if (!dateVal) return '—';
    const d = new Date(dateVal);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleString('en-IN', {
      timeZone: 'Asia/Kolkata',
      day: '2-digit',
      month: 'short',
      year: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }
}
