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

  selectedExpenseId = ''; referenceNo = ''; paymentMode = 'BankTransfer';
  role: string = ''; loading = false;
  searchExpenseId = ''; searchResults: any[] = []; searching = false;

  // Filters & Sort
  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  sortBy: 'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  // Pagination
  page = 1; pageSize = 6;

  // Payment Detail Modal
  showPaymentModal = false; modalPaymentDetails: any = null;

  // File View Modal
  showFileModal = false; modalFileUrls: string[] = [];

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
    this.api.getAllExpenses(1, 200).subscribe({
      next: (res) => { const all = res.data ?? res ?? []; this.approvedExpenses = all.filter((e: any) => e.status === 'Approved'); },
      error: () => {}
    });
  }

  completePayment() {
    if (!this.isFinance()) { this.toast.showError('Only Finance users can complete payments.'); return; }
    if (!this.selectedExpenseId) { this.toast.showWarning('Please select an expense to pay.'); return; }
    if (!this.referenceNo.trim()) { this.toast.showWarning('Reference number is required.'); return; }
    this.loader.show();
    this.api.completePayment(this.selectedExpenseId, { referenceNo: this.referenceNo, paymentMode: this.paymentMode }).subscribe({
      next: () => {
        this.toast.show('Payment completed successfully 💰');
        this.selectedExpenseId = ''; this.referenceNo = '';
        this.loadPayments(); this.loadApprovedExpenses(); this.loader.hide();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to complete payment.'); this.loader.hide(); }
    });
  }

  loadPayments() {
    this.loading = true; this.loader.show();
    this.api.getAllPayments(1, 200).subscribe({
      next: (res: any) => {
        this.payments = res.data ?? res ?? [];
        this.applyFilters();
        this.loading = false; this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load payments.'); this.loading = false; this.loader.hide(); }
    });
  }

  applyFilters() {
    let data = [...this.payments];
    if (this.filterStatus) data = data.filter(p => p.paymentStatus === this.filterStatus);
    if (this.filterMinAmount !== null) data = data.filter(p => p.amountPaid >= this.filterMinAmount!);
    if (this.filterMaxAmount !== null) data = data.filter(p => p.amountPaid <= this.filterMaxAmount!);
    if (this.filterDateFrom) data = data.filter(p => new Date(p.paymentDate) >= new Date(this.filterDateFrom));
    if (this.filterDateTo)   data = data.filter(p => new Date(p.paymentDate) <= new Date(this.filterDateTo + 'T23:59:59'));
    if (this.sortBy === 'amount') data.sort((a, b) => this.sortDir === 'asc' ? a.amountPaid - b.amountPaid : b.amountPaid - a.amountPaid);
    if (this.sortBy === 'date')   data.sort((a, b) => this.sortDir === 'asc' ? new Date(a.paymentDate).getTime() - new Date(b.paymentDate).getTime() : new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime());
    this.filteredPayments = data;
    this.page = 1;
    this.updatePage();
  }

  updatePage() {
    const start = (this.page - 1) * this.pageSize;
    this.pagedPayments = this.filteredPayments.slice(start, start + this.pageSize);
  }

  totalPages() { return Math.ceil(this.filteredPayments.length / this.pageSize); }
  nextPage() { if (this.page < this.totalPages()) { this.page++; this.updatePage(); } }
  prevPage() { if (this.page > 1) { this.page--; this.updatePage(); } }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) { this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.applyFilters();
  }

  clearFilters() { this.filterStatus = ''; this.filterDateFrom = ''; this.filterDateTo = ''; this.filterMinAmount = null; this.filterMaxAmount = null; this.sortBy = ''; this.sortDir = 'desc'; this.applyFilters(); }

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
  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
