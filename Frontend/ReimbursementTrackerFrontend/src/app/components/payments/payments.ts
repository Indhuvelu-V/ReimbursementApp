import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { TokenService } from '../../services/token.service';
import { FileViewModal } from '../file-view-modal/file-view-modal';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, FileViewModal],
  templateUrl: './payments.html',
  styleUrls: ['./payments.css']
})
export class Payments implements OnInit {
  private api          = inject(APIService);
  private toast        = inject(ToastService);
  private loader       = inject(LoaderService);
  private tokenService = inject(TokenService);

  payments: any[] = [];
  filteredPayments: any[] = [];
  pagedPayments: any[] = [];
  approvedExpenses: any[] = [];
  approvedTotal = 0;
  approvedPage = 1; approvedPageSize = 6;

  // Awaiting list filters
  awaitingFilterDateFrom = '';
  awaitingFilterDateTo = '';
  awaitingFilterMinAmount: number | null = null;
  awaitingFilterMaxAmount: number | null = null;
  awaitingFilterUserName = '';

  role: string = ''; loading = false;
  searchExpenseId = ''; searchResults: any[] = []; searching = false;

  // Filters & Sort
  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  filterUserName = '';
  filterProcessedBy = '';   // Finance user who processed the payment
  sortBy: 'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  // Server-side pagination
  page = 1; pageSize = 6;
  totalRecords = 0;

  // Inline complete-payment modal (triggered from awaiting list)
  showCompleteModal = false;
  completeExp: any = null;
  referenceNo = '';
  paymentMode = 'BankTransfer';

  // Expense detail modal (for Details button in awaiting list)
  showExpenseModal = false;
  modalExpenseDetails: any = null;

  // Payment Detail Modal
  showPaymentModal = false; modalPaymentDetails: any = null;

  // File View Modal
  showFileModal = false; modalFileUrls: string[] = [];

  // Tab for finance view
  financeTab: 'complete' | 'history' = 'complete';
  switchFinanceTab(tab: 'complete' | 'history') { this.financeTab = tab; }

  constructor(public apiSvc: APIService) {}

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    if (this.isFinanceOrAdmin()) { this.loadPayments(); this.loadApprovedExpenses(); }
  }

  isEmployee(): boolean     { return this.role?.toLowerCase() === 'employee'; }
  isManager(): boolean      { return this.role?.toLowerCase() === 'manager'; }
  isFinanceOrAdmin(): boolean { const r = this.role?.toLowerCase(); return r === 'finance' || r === 'admin'; }
  isFinance(): boolean      { return this.role?.toLowerCase() === 'finance'; }

  loadApprovedExpenses() {
    this.api.getAllExpenses(
      this.approvedPage, this.approvedPageSize,
      'Approved',
      this.awaitingFilterDateFrom || undefined,
      this.awaitingFilterDateTo || undefined,
      this.awaitingFilterMinAmount,
      this.awaitingFilterMaxAmount,
      this.awaitingFilterUserName || undefined
    ).subscribe({
      next: (res) => {
        let all: any[] = res.data ?? res ?? [];
        // Client-side username filter as fallback
        if (this.awaitingFilterUserName.trim()) {
          const q = this.awaitingFilterUserName.trim().toLowerCase();
          all = all.filter(e =>
            (e.userName ?? '').toLowerCase().includes(q) ||
            (e.expenseId ?? '').toLowerCase().includes(q)
          );
        }
        this.approvedExpenses = all;
        this.approvedTotal = res.totalRecords ?? all.length;
      },
      error: () => {}
    });
  }

  applyAwaitingFilters() { this.approvedPage = 1; this.loadApprovedExpenses(); }
  clearAwaitingFilters() {
    this.awaitingFilterDateFrom = ''; this.awaitingFilterDateTo = '';
    this.awaitingFilterMinAmount = null; this.awaitingFilterMaxAmount = null;
    this.awaitingFilterUserName = '';
    this.approvedPage = 1; this.loadApprovedExpenses();
  }
  approvedTotalPages() { return Math.ceil(this.approvedTotal / this.approvedPageSize); }
  approvedNextPage() { if (this.approvedPage < this.approvedTotalPages()) { this.approvedPage++; this.loadApprovedExpenses(); } }
  approvedPrevPage() { if (this.approvedPage > 1) { this.approvedPage--; this.loadApprovedExpenses(); } }

  // Open inline modal for a specific expense
  openCompleteModal(exp: any) {
    this.completeExp = exp;
    this.referenceNo = '';
    this.paymentMode = 'BankTransfer';
    this.showCompleteModal = true;
  }

  closeCompleteModal() {
    this.showCompleteModal = false;
    this.completeExp = null;
    this.referenceNo = '';
  }

  completePayment() {
    if (!this.isFinance()) { this.toast.showError('Only Finance users can complete payments.'); return; }
    if (!this.completeExp?.expenseId) { this.toast.showWarning('No expense selected.'); return; }
    if (!this.referenceNo.trim()) { this.toast.showWarning('Reference number is required.'); return; }
    this.loader.show();
    this.api.completePayment(this.completeExp.expenseId, { referenceNo: this.referenceNo, paymentMode: this.paymentMode }).subscribe({
      next: () => {
        this.toast.show('Payment completed successfully 💰');
        this.closeCompleteModal();
        this.loadPayments(); this.loadApprovedExpenses(); this.loader.hide();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to complete payment.'); this.loader.hide(); }
    });
  }

  loadPayments() {
    this.loading = true; this.loader.show();
    this.api.getAllPayments(
      this.page, this.pageSize,
      this.filterDateFrom || undefined,
      this.filterDateTo || undefined,
      this.filterMinAmount,
      this.filterMaxAmount,
      this.filterStatus || undefined
      // userName sent separately — filtered client-side after load for reliability
    ).subscribe({
      next: (res: any) => {
        const raw: any[] = res.data ?? res ?? [];
        this.totalRecords = res.totalRecords ?? raw.length;
        let data = [...raw];
        // Client-side username filter (case-insensitive contains)
        if (this.filterUserName.trim()) {
          const q = this.filterUserName.trim().toLowerCase();
          data = data.filter(p =>
            (p.userName ?? '').toLowerCase().includes(q) ||
            (p.expenseId ?? '').toLowerCase().includes(q)
          );
        }
        if (this.filterProcessedBy.trim()) {
          const q = this.filterProcessedBy.trim().toLowerCase();
          data = data.filter(p => (p.processedByName ?? '').toLowerCase().includes(q));
        }
        if (this.sortBy === 'amount') data.sort((a, b) => this.sortDir === 'asc' ? a.amountPaid - b.amountPaid : b.amountPaid - a.amountPaid);
        if (this.sortBy === 'date')   data.sort((a, b) => this.sortDir === 'asc' ? new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime() : new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime());
        this.filteredPayments = data;
        this.pagedPayments = data;
        this.loading = false; this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load payments.'); this.loading = false; this.loader.hide(); }
    });
  }

  applyFilters() { this.page = 1; this.loadPayments(); }

  totalPages() { return Math.ceil(this.totalRecords / this.pageSize); }
  nextPage() { if (this.page < this.totalPages()) { this.page++; this.loadPayments(); } }
  prevPage() { if (this.page > 1) { this.page--; this.loadPayments(); } }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) { this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.loadPayments();
  }

  clearFilters() {
    this.filterStatus = ''; this.filterDateFrom = ''; this.filterDateTo = '';
    this.filterMinAmount = null; this.filterMaxAmount = null;
    this.filterUserName = ''; this.filterProcessedBy = '';
    this.sortBy = ''; this.sortDir = 'desc'; this.page = 1;
    this.loadPayments();
  }

  searchPaymentByExpenseId() {
    if (!this.searchExpenseId.trim()) { this.toast.showWarning('Please enter an Expense ID.'); return; }
    this.searching = true; this.loader.show();
    this.api.getPaymentByExpenseId(this.searchExpenseId.trim()).subscribe({
      next: (res: any) => {
        this.loader.hide(); this.searching = false;
        const payment = res?.data ?? res;
        if (payment) { this.searchResults = [payment]; this.toast.show('Payment found ✅'); }
        else { this.searchResults = []; }
      },
      error: (err) => { this.loader.hide(); this.searching = false; this.searchResults = []; this.toast.showError(err?.status === 404 ? 'No payment found.' : 'Failed to fetch payment.'); }
    });
  }

  viewPaymentDetails(expenseId: string) {
    this.loader.show();
    this.api.getPaymentByExpenseId(expenseId).subscribe({
      next: (res: any) => {
        this.loader.hide();
        const payment = res?.data ?? res;
        if (payment && payment.paymentStatus === 'Paid') { this.modalPaymentDetails = payment; this.showPaymentModal = true; }
        else { this.toast.showInfo('No completed payment found for this expense.'); }
      },
      error: () => { this.loader.hide(); this.toast.showError('Payment not found.'); }
    });
  }

  closePaymentModal() { this.showPaymentModal = false; this.modalPaymentDetails = null; }

  // Show expense details directly (for awaiting list Details button)
  viewExpenseDetails(exp: any) { this.modalExpenseDetails = exp; this.showExpenseModal = true; }
  closeExpenseModal() { this.showExpenseModal = false; this.modalExpenseDetails = null; }

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
