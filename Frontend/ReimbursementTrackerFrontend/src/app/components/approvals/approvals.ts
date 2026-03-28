import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TokenService } from '../../services/token.service';
import { ToastService } from '../../services/toast.service';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { CreateApprovalRequestDto } from '../../models/approval.model';
import { FileViewModal } from '../file-view-modal/file-view-modal';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, FileViewModal],
  templateUrl: './approvals.html',
  styleUrls: ['./approvals.css']
})
export class Approvals implements OnInit {
  private api    = inject(APIService);
  private token  = inject(TokenService);
  private toast  = inject(ToastService);
  private loader = inject(LoaderService);

  approvals: any[] = [];
  filteredApprovals: any[] = [];
  pagedApprovals: any[] = [];
  submittedExpenses: any[] = [];
  filteredSubmitted: any[] = [];   // filtered view for manager
  pagedSubmitted: any[] = [];
  loading = false;
  role: string = '';

  // Per-card inline comment state
  cardComments: Record<string, string> = {};
  submittingId: string | null = null;

  // Admin filters (approvals list)
  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  filterUserName = '';       // expense submitter (employee)
  filterApproverName = '';   // manager who approved
  sortBy: 'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  // Manager filters (submitted expenses)
  mgrFilterDateFrom = '';
  mgrFilterDateTo = '';
  mgrFilterMinAmount: number | null = null;
  mgrFilterMaxAmount: number | null = null;
  mgrFilterUserName = '';
  mgrSortBy: 'amount' | 'date' | '' = '';
  mgrSortDir: 'asc' | 'desc' = 'desc';

  page = 1; pageSize = 6;
  mgrPage = 1; mgrPageSize = 6;

  // Tab for manager view
  mgrTab: 'pending' | 'history' = 'pending';
  switchMgrTab(tab: 'pending' | 'history') { this.mgrTab = tab; }

  showFileModal = false; modalFileUrls: string[] = [];

  constructor(public apiSvc: APIService) {}

  ngOnInit(): void {
    this.role = this.token.getRoleFromToken() ?? '';
    this.loadApprovals();
    if (this.role.toLowerCase() === 'manager') this.loadSubmittedExpenses();
  }

  loadApprovals(): void {
    this.loading = true;
    const r = this.role.toLowerCase();
    if (r === 'admin' || r === 'manager') {
      this.loader.show();
      this.api.getAllApprovals({ pageNumber: 1, pageSize: 200 }).subscribe({
        next: (res) => {
          let data = res.data ?? [];
          // Separate filters for user (employee) and approver
          if (this.filterUserName.trim()) {
            const q = this.filterUserName.trim().toLowerCase();
            data = data.filter((a: any) =>
              (a.employeeName ?? '').toLowerCase().includes(q) ||
              (a.expenseId ?? '').toLowerCase().includes(q)
            );
          }
          if (this.filterApproverName.trim()) {
            const q = this.filterApproverName.trim().toLowerCase();
            data = data.filter((a: any) =>
              (a.approverName ?? '').toLowerCase().includes(q)
            );
          }
          if (this.filterStatus) data = data.filter((a: any) => a.status === this.filterStatus);
          if (this.filterMinAmount !== null) data = data.filter((a: any) => Number(a.expenseAmount) >= Number(this.filterMinAmount));
          if (this.filterMaxAmount !== null) data = data.filter((a: any) => Number(a.expenseAmount) <= Number(this.filterMaxAmount));
          if (this.filterDateFrom) data = data.filter((a: any) => new Date(a.approvedAt) >= new Date(this.filterDateFrom));
          if (this.filterDateTo)   data = data.filter((a: any) => new Date(a.approvedAt) <= new Date(this.filterDateTo + 'T23:59:59'));
          if (this.sortBy === 'amount') data.sort((a: any, b: any) => this.sortDir === 'asc' ? Number(a.expenseAmount) - Number(b.expenseAmount) : Number(b.expenseAmount) - Number(a.expenseAmount));
          if (this.sortBy === 'date')   data.sort((a: any, b: any) => this.sortDir === 'asc' ? new Date(a.approvedAt).getTime() - new Date(b.approvedAt).getTime() : new Date(b.approvedAt).getTime() - new Date(a.approvedAt).getTime());
          this.approvals = res.data ?? [];
          this.filteredApprovals = data;
          this.page = 1;
          this.updatePage();
          this.loading = false; this.loader.hide();
        },
        error: () => { this.toast.showError('Failed to load approvals.'); this.loading = false; this.loader.hide(); }
      });
    } else {
      this.approvals = []; this.filteredApprovals = []; this.loading = false;
    }
  }

  loadSubmittedExpenses(): void {
    const managerId = this.token.getUserIdFromToken();
    this.api.getAllExpenses(1, 200, 'Submitted', undefined, undefined, undefined, undefined, this.mgrFilterUserName || undefined).subscribe({
      next: (res) => {
        const all = res.data ?? res ?? [];
        this.submittedExpenses = all.filter((e: any) =>
          e.status === 'Submitted' && e.userId !== managerId
        );
        this.submittedExpenses.forEach(e => {
          if (!(e.expenseId in this.cardComments)) this.cardComments[e.expenseId] = '';
        });
        this.applyManagerFilters();
      },
      error: () => {}
    });
  }

  applyManagerFilters() {
    let data = [...this.submittedExpenses];

    // Amount — coerce to number since ngModel on number input can return string
    const minAmt = this.mgrFilterMinAmount !== null ? Number(this.mgrFilterMinAmount) : null;
    const maxAmt = this.mgrFilterMaxAmount !== null ? Number(this.mgrFilterMaxAmount) : null;
    if (minAmt !== null && !isNaN(minAmt)) data = data.filter(e => Number(e.amount) >= minAmt);
    if (maxAmt !== null && !isNaN(maxAmt)) data = data.filter(e => Number(e.amount) <= maxAmt);

    // Date — expenseDate arrives as "dd-MM-yyyy", parse it before comparing
    if (this.mgrFilterDateFrom) {
      const from = new Date(this.mgrFilterDateFrom);
      data = data.filter(e => this.parseExpenseDate(e.expenseDate) >= from);
    }
    if (this.mgrFilterDateTo) {
      const to = new Date(this.mgrFilterDateTo + 'T23:59:59');
      data = data.filter(e => this.parseExpenseDate(e.expenseDate) <= to);
    }

    if (this.mgrSortBy === 'amount') data.sort((a, b) => this.mgrSortDir === 'asc' ? Number(a.amount) - Number(b.amount) : Number(b.amount) - Number(a.amount));
    if (this.mgrSortBy === 'date')   data.sort((a, b) => this.mgrSortDir === 'asc' ? this.parseExpenseDate(a.expenseDate).getTime() - this.parseExpenseDate(b.expenseDate).getTime() : this.parseExpenseDate(b.expenseDate).getTime() - this.parseExpenseDate(a.expenseDate).getTime());

    this.filteredSubmitted = data;
    this.mgrPage = 1;
    this.updateMgrPage();
  }

  // Parse "dd-MM-yyyy" or "yyyy-MM-dd" into a Date
  private parseExpenseDate(dateStr: string): Date {
    if (!dateStr) return new Date(0);
    const parts = dateStr.split('-');
    if (parts.length === 3 && parts[0].length === 2) {
      // dd-MM-yyyy → yyyy-MM-dd
      return new Date(`${parts[2]}-${parts[1]}-${parts[0]}`);
    }
    return new Date(dateStr);
  }

  updateMgrPage() {
    const s = (this.mgrPage - 1) * this.mgrPageSize;
    this.pagedSubmitted = this.filteredSubmitted.slice(s, s + this.mgrPageSize);
  }

  mgrTotalPages() { return Math.ceil(this.filteredSubmitted.length / this.mgrPageSize); }
  mgrNextPage() { if (this.mgrPage < this.mgrTotalPages()) { this.mgrPage++; this.updateMgrPage(); } }
  mgrPrevPage() { if (this.mgrPage > 1) { this.mgrPage--; this.updateMgrPage(); } }

  toggleMgrSort(field: 'amount' | 'date') {
    if (this.mgrSortBy === field) { this.mgrSortDir = this.mgrSortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.mgrSortBy = field; this.mgrSortDir = 'desc'; }
    this.applyManagerFilters();
  }

  clearMgrFilters() {
    this.mgrFilterDateFrom = ''; this.mgrFilterDateTo = '';
    this.mgrFilterMinAmount = null; this.mgrFilterMaxAmount = null;
    this.mgrFilterUserName = '';
    this.mgrSortBy = ''; this.mgrSortDir = 'desc';
    this.applyManagerFilters();
  }

  // Inline approve/reject directly from card
  decideInline(expenseId: string, status: 'approved' | 'rejected'): void {
    const managerId = this.token.getUserIdFromToken();
    if (!managerId) { this.toast.showError('User ID not found in token.'); return; }
    const request: CreateApprovalRequestDto = {
      expenseId,
      managerId,
      status,
      comments: this.cardComments[expenseId] ?? '',
      level: 'Manager'
    };
    this.submittingId = expenseId;
    this.loader.show();
    this.api.managerApproval(request).subscribe({
      next: () => {
        this.toast.show(`Expense ${status === 'approved' ? 'Approved ✅' : 'Rejected ❌'} successfully`);
        delete this.cardComments[expenseId];
        this.submittingId = null;
        this.loadSubmittedExpenses();
        this.loader.hide();
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Action failed.');
        this.submittingId = null;
        this.loader.hide();
      }
    });
  }

  applyFilters() {
    this.loadApprovals();
  }

  updatePage() { const s = (this.page - 1) * this.pageSize; this.pagedApprovals = this.filteredApprovals.slice(s, s + this.pageSize); }
  totalPages() { return Math.ceil(this.filteredApprovals.length / this.pageSize); }
  nextPage() { if (this.page < this.totalPages()) { this.page++; this.updatePage(); } }
  prevPage() { if (this.page > 1) { this.page--; this.updatePage(); } }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) { this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.loadApprovals();
  }

  clearFilters() { this.filterStatus = ''; this.filterDateFrom = ''; this.filterDateTo = ''; this.filterMinAmount = null; this.filterMaxAmount = null; this.filterUserName = ''; this.filterApproverName = ''; this.sortBy = ''; this.sortDir = 'desc'; this.applyFilters(); }

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
