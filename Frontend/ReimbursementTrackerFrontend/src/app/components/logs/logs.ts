import { Component, OnInit } from '@angular/core';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { ToastService } from '../../services/toast.service';
import { TokenService } from '../../services/token.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
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

  logs:         any[] = [];   // unused, kept for compatibility
  filteredLogs: any[] = [];   // current page data from server
  pagedLogs:    any[] = [];   // same as filteredLogs (server paginates)
  role:         string | null = null;
  page   = 1;
  size   = 10;
  total  = 0;

  showDeleteConfirm = false;
  pendingDeleteId:  string | null = null;
  pendingDeleteLog: any = null;

  showBulkDeleteConfirm = false;

  fromDate:     string = '';
  toDate:       string = '';
  filterAction: string = '';
  filterUserName: string = '';

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

  loadLogs() {
    this.loader.show();
    this.api.getPagedLogs(
      this.page,
      this.size,
      this.fromDate || undefined,
      this.toDate || undefined,
      this.filterUserName.trim() || undefined,
      this.filterAction || undefined
    ).subscribe({
      next: (res: PagedResponse<CreateAuditLogsResponseDto>) => {
        this.filteredLogs = res.data ?? (res as any).Data ?? [];
        this.pagedLogs    = this.filteredLogs;
        this.total        = res.totalRecords ?? (res as any).TotalRecords ?? res.data?.length ?? 0;
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load audit logs.'); this.loader.hide(); }
    });
  }

  applyLocalFilter() {
    // No longer needed — all filtering is server-side
    // Kept for bulk delete compatibility (filteredLogs = current page)
  }

  updatePage() {
    // Server handles pagination — just reload
    this.loadLogs();
  }

  applyFilter() {
    this.page = 1;
    this.loadLogs();
  }

  applyUserNameFilter() {
    this.page = 1;
    this.loadLogs();
  }

  clearFilter() {
    this.fromDate = ''; this.toDate = '';
    this.filterAction = ''; this.filterUserName = '';
    this.page = 1;
    this.loadLogs();
  }

  confirmDelete(log: any) {
    this.pendingDeleteId  = log.logId;
    this.pendingDeleteLog = log;
    this.showDeleteConfirm = true;
  }

  cancelDelete() {
    this.showDeleteConfirm = false;
    this.pendingDeleteId  = null;
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
        this.loadLogs();
        this.loader.hide();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to delete log.'); this.loader.hide(); }
    });
  }

  // ── Bulk delete filtered ──────────────────────────────────────────────────

  get hasActiveFilter(): boolean {
    return !!(this.fromDate || this.toDate || this.filterAction || this.filterUserName.trim());
  }

  confirmBulkDelete() { this.showBulkDeleteConfirm = true; }
  cancelBulkDelete()  { this.showBulkDeleteConfirm = false; }

  executeBulkDelete() {
    this.showBulkDeleteConfirm = false;
    // Fetch ALL filtered records (all pages) to get their IDs, then delete
    this.loader.show();
    this.api.getPagedLogs(1, 10000,
      this.fromDate || undefined, this.toDate || undefined,
      this.filterUserName.trim() || undefined, this.filterAction || undefined
    ).subscribe({
      next: (res) => {
        const ids = (res.data ?? []).map((l: any) => l.logId).filter(Boolean);
        if (!ids.length) { this.loader.hide(); return; }
        forkJoin(ids.map((id: string) => this.api.deleteLog(id))).subscribe({
          next: () => {
            this.toast.show(`${ids.length} log${ids.length !== 1 ? 's' : ''} deleted ✅`);
            this.page = 1;
            this.loadLogs();
          },
          error: () => { this.toast.showError('Some logs could not be deleted.'); this.loadLogs(); this.loader.hide(); }
        });
      },
      error: () => { this.toast.showError('Failed to fetch logs for deletion.'); this.loader.hide(); }
    });
  }

  nextPage() { if (this.page < this.totalPages()) { this.page++; this.loadLogs(); } }
  prevPage() { if (this.page > 1) { this.page--; this.loadLogs(); } }
  totalPages(): number { return Math.ceil(this.total / this.size); }

  // ── Display helpers ───────────────────────────────────────────────────────

  actionLabel(action: string = ''): string {
    const a = action.toLowerCase();
    if (a.includes('creat') || a.includes('added')) return 'CREATE';
    if (a.includes('updat') || a.includes('edit'))  return 'UPDATE';
    if (a.includes('delet') || a.includes('remov')) return 'DELETE';
    if (a.includes('submit') || a.includes('resubmit')) return 'SUBMIT';
    if (a.includes('approv')) return 'APPROVE';
    if (a.includes('reject')) return 'REJECT';
    if (a.includes('paid') || a.includes('payment')) return 'PAYMENT';
    if (a.includes('view') || a.includes('list'))   return 'VIEW';
    return 'ACTION';
  }

  actionBadgeClass(action: string = ''): string {
    const label = this.actionLabel(action);
    const map: Record<string, string> = {
      'CREATE':  'badge-action-create',
      'UPDATE':  'badge-action-update',
      'DELETE':  'badge-action-delete',
      'SUBMIT':  'badge-action-submit',
      'APPROVE': 'badge-action-approve',
      'REJECT':  'badge-action-reject',
      'PAYMENT': 'badge-action-payment',
      'VIEW':    'badge-action-view',
    };
    return map[label] ?? 'badge-action-default';
  }

  actionDotClass(action: string = ''): string {
    return 'dot-' + this.actionBadgeClass(action).replace('badge-action-', '');
  }

  actionIcon(action: string = ''): string {
    const label = this.actionLabel(action);
    const map: Record<string, string> = {
      'CREATE':  'bi-plus-lg',
      'UPDATE':  'bi-pencil',
      'DELETE':  'bi-trash3',
      'SUBMIT':  'bi-send',
      'APPROVE': 'bi-check-lg',
      'REJECT':  'bi-x-lg',
      'PAYMENT': 'bi-cash-stack',
      'VIEW':    'bi-eye',
    };
    return map[label] ?? 'bi-dot';
  }

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
      timeZone: 'Asia/Kolkata', day: '2-digit', month: 'short',
      year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true
    });
  }
}
