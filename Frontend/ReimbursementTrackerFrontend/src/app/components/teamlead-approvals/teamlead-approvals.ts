import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TokenService } from '../../services/token.service';
import { ToastService } from '../../services/toast.service';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { FileViewModal } from '../file-view-modal/file-view-modal';
import { CreateApprovalRequestDto } from '../../models/approval.model';

@Component({
  selector: 'app-teamlead-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, FileViewModal],
  templateUrl: './teamlead-approvals.html',
  styleUrls: ['./teamlead-approvals.css']
})
export class TeamLeadApprovals implements OnInit {
  private api    = inject(APIService);
  private token  = inject(TokenService);
  private toast  = inject(ToastService);
  private loader = inject(LoaderService);

  // Pending tab — expenses with status PendingTeamLead
  pendingExpenses:  any[] = [];
  filteredPending:  any[] = [];
  pagedPending:     any[] = [];

  // History tab — approvals made by this team lead
  historyApprovals:  any[] = [];
  filteredHistory:   any[] = [];
  pagedHistory:      any[] = [];

  tab: 'pending' | 'history' = 'pending';
  cardComments: Record<string, string> = {};
  submittingId: string | null = null;

  // Pending filters
  filterUserName   = '';
  filterDateFrom   = '';
  filterDateTo     = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  sortBy:  'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  // History filters
  hFilterStatus    = '';
  hFilterUserName  = '';
  hFilterDateFrom  = '';
  hFilterDateTo    = '';
  hFilterMinAmount: number | null = null;
  hFilterMaxAmount: number | null = null;
  hSortBy:  'amount' | 'date' | '' = '';
  hSortDir: 'asc' | 'desc' = 'desc';

  page = 1; pageSize = 6;
  hPage = 1; hPageSize = 6;

  showFileModal = false; modalFileUrls: string[] = [];

  ngOnInit() {
    this.loadPending();
    this.loadHistory();
  }

  switchTab(t: 'pending' | 'history') { this.tab = t; }

  // ── Load expenses waiting for Team Lead approval (same dept, via backend) ─
  loadPending() {
    const myId = this.token.getUserIdFromToken();
    this.loader.show();
    // Use the dedicated pending endpoint — backend filters by dept + role automatically
    this.api.getPendingApprovalsForMe().subscribe({
      next: (res: any[]) => {
        this.pendingExpenses = res;
        this.pendingExpenses.forEach((e: any) => {
          if (!(e.expenseId in this.cardComments)) this.cardComments[e.expenseId] = '';
        });
        this.applyPendingFilters();
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load pending approvals.'); this.loader.hide(); }
    });
  }

  // ── Load approval history for this team lead ──────────────────────────────
  loadHistory() {
    this.api.getMyApprovalHistory().subscribe({
      next: (res: any[]) => {
        this.historyApprovals = res ?? [];
        this.applyHistoryFilters();
      },
      error: () => {}
    });
  }

  // ── Approve / Reject inline ───────────────────────────────────────────────
  decide(expenseId: string, status: 'approved' | 'rejected') {
    const approverId = this.token.getUserIdFromToken();
    if (!approverId) { this.toast.showError('User ID not found.'); return; }

    const request: CreateApprovalRequestDto = {
      expenseId,
      managerId: approverId,
      status,
      comments: this.cardComments[expenseId] ?? '',
      level: 'TeamLead'
    };

    this.submittingId = expenseId;
    this.loader.show();

    this.api.teamLeadApproval(request).subscribe({
      next: () => {
        this.toast.show(status === 'approved'
          ? 'Approved ✅ — forwarded to Manager'
          : 'Rejected ❌');
        delete this.cardComments[expenseId];
        this.submittingId = null;
        this.loadPending();
        this.loadHistory();
        this.loader.hide();
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Action failed.');
        this.submittingId = null;
        this.loader.hide();
      }
    });
  }

  // ── Pending filters ───────────────────────────────────────────────────────
  applyPendingFilters() {
    let data = [...this.pendingExpenses];
    if (this.filterUserName.trim()) {
      const q = this.filterUserName.trim().toLowerCase();
      data = data.filter(e => (e.userName ?? '').toLowerCase().includes(q));
    }
    const minA = this.filterMinAmount !== null ? Number(this.filterMinAmount) : null;
    const maxA = this.filterMaxAmount !== null ? Number(this.filterMaxAmount) : null;
    if (minA !== null && !isNaN(minA)) data = data.filter(e => Number(e.amount) >= minA);
    if (maxA !== null && !isNaN(maxA)) data = data.filter(e => Number(e.amount) <= maxA);
    if (this.filterDateFrom) {
      const from = new Date(this.filterDateFrom);
      data = data.filter(e => this.parseDate(e.expenseDate) >= from);
    }
    if (this.filterDateTo) {
      const to = new Date(this.filterDateTo + 'T23:59:59');
      data = data.filter(e => this.parseDate(e.expenseDate) <= to);
    }
    if (this.sortBy === 'amount') data.sort((a, b) => this.sortDir === 'asc' ? Number(a.amount) - Number(b.amount) : Number(b.amount) - Number(a.amount));
    if (this.sortBy === 'date')   data.sort((a, b) => this.sortDir === 'asc' ? this.parseDate(a.expenseDate).getTime() - this.parseDate(b.expenseDate).getTime() : this.parseDate(b.expenseDate).getTime() - this.parseDate(a.expenseDate).getTime());
    this.filteredPending = data;
    this.page = 1;
    this.updatePage();
  }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.applyPendingFilters();
  }

  clearPendingFilters() {
    this.filterUserName = ''; this.filterDateFrom = ''; this.filterDateTo = '';
    this.filterMinAmount = null; this.filterMaxAmount = null;
    this.sortBy = ''; this.sortDir = 'desc';
    this.applyPendingFilters();
  }

  updatePage() { const s = (this.page - 1) * this.pageSize; this.pagedPending = this.filteredPending.slice(s, s + this.pageSize); }
  totalPages() { return Math.ceil(this.filteredPending.length / this.pageSize); }
  nextPage()   { if (this.page < this.totalPages()) { this.page++; this.updatePage(); } }
  prevPage()   { if (this.page > 1) { this.page--; this.updatePage(); } }

  // ── History filters ───────────────────────────────────────────────────────
  applyHistoryFilters() {
    let data = [...this.historyApprovals];
    if (this.hFilterStatus)   data = data.filter(a => a.status === this.hFilterStatus);
    if (this.hFilterUserName.trim()) {
      const q = this.hFilterUserName.trim().toLowerCase();
      data = data.filter(a => (a.employeeName ?? '').toLowerCase().includes(q));
    }
    const minA = this.hFilterMinAmount !== null ? Number(this.hFilterMinAmount) : null;
    const maxA = this.hFilterMaxAmount !== null ? Number(this.hFilterMaxAmount) : null;
    if (minA !== null && !isNaN(minA)) data = data.filter(a => Number(a.expenseAmount) >= minA);
    if (maxA !== null && !isNaN(maxA)) data = data.filter(a => Number(a.expenseAmount) <= maxA);
    if (this.hFilterDateFrom) data = data.filter(a => new Date(a.approvedAt) >= new Date(this.hFilterDateFrom));
    if (this.hFilterDateTo)   data = data.filter(a => new Date(a.approvedAt) <= new Date(this.hFilterDateTo + 'T23:59:59'));
    if (this.hSortBy === 'amount') data.sort((a, b) => this.hSortDir === 'asc' ? Number(a.expenseAmount) - Number(b.expenseAmount) : Number(b.expenseAmount) - Number(a.expenseAmount));
    if (this.hSortBy === 'date')   data.sort((a, b) => this.hSortDir === 'asc' ? new Date(a.approvedAt).getTime() - new Date(b.approvedAt).getTime() : new Date(b.approvedAt).getTime() - new Date(a.approvedAt).getTime());
    this.filteredHistory = data;
    this.hPage = 1;
    this.updateHPage();
  }

  toggleHSort(field: 'amount' | 'date') {
    if (this.hSortBy === field) this.hSortDir = this.hSortDir === 'asc' ? 'desc' : 'asc';
    else { this.hSortBy = field; this.hSortDir = 'desc'; }
    this.applyHistoryFilters();
  }

  clearHistoryFilters() {
    this.hFilterStatus = ''; this.hFilterUserName = '';
    this.hFilterDateFrom = ''; this.hFilterDateTo = '';
    this.hFilterMinAmount = null; this.hFilterMaxAmount = null;
    this.hSortBy = ''; this.hSortDir = 'desc';
    this.applyHistoryFilters();
  }

  updateHPage() { const s = (this.hPage - 1) * this.hPageSize; this.pagedHistory = this.filteredHistory.slice(s, s + this.hPageSize); }
  hTotalPages() { return Math.ceil(this.filteredHistory.length / this.hPageSize); }
  hNextPage()   { if (this.hPage < this.hTotalPages()) { this.hPage++; this.updateHPage(); } }
  hPrevPage()   { if (this.hPage > 1) { this.hPage--; this.updateHPage(); } }

  // ── Helpers ───────────────────────────────────────────────────────────────
  private parseDate(dateStr: string): Date {
    if (!dateStr) return new Date(0);
    const parts = dateStr.split('-');
    if (parts.length === 3 && parts[0].length === 2)
      return new Date(`${parts[2]}-${parts[1]}-${parts[0]}`);
    return new Date(dateStr);
  }

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
