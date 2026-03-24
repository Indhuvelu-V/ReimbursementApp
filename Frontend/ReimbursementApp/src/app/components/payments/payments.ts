import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { TokenService } from '../../services/token.service';
import { CreatePaymentResponseDto } from '../../models/payment.model';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payments.html',
  styleUrls: ['./payments.css']
})
export class Payments implements OnInit {
  private api          = inject(APIService);
  private toast        = inject(ToastService);
  private loader       = inject(LoaderService);
  private tokenService = inject(TokenService);

  payments: CreatePaymentResponseDto[] = [];
  approvedExpenses: any[] = [];
  selectedExpenseId = '';
  referenceNo       = '';
  paymentMode       = 'BankTransfer';
  role: string      = '';
  loading           = false;

  // Search
  searchExpenseId = '';
  searchResults: CreatePaymentResponseDto[] = [];
  searching = false;

  // Modal
  showPaymentModal = false;
  modalPaymentDetails: any = null;

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    if (this.isFinanceOrAdmin()) {
      this.loadPayments();
      this.loadApprovedExpenses();
    }
  }

  // ================= ROLE HELPERS =================
  isEmployee(): boolean { return this.role?.toLowerCase() === 'employee'; }
  isManager(): boolean { return this.role?.toLowerCase() === 'manager'; }
  isFinanceOrAdmin(): boolean {
    const r = this.role?.toLowerCase();
    return r === 'finance' || r === 'admin';
  }
  isFinance(): boolean { return this.role?.toLowerCase() === 'finance'; }

  // ================= LOAD APPROVED EXPENSES =================
  loadApprovedExpenses() {
    this.api.getAllExpenses(1, 100).subscribe({
      next: (res) => {
        const all = res.data ?? res ?? [];
        this.approvedExpenses = all.filter((e: any) => e.status === 'Approved');
      },
      error: () => {}
    });
  }

  // ================= COMPLETE PAYMENT =================
  completePayment() {
    if (!this.isFinance()) { this.toast.showError('Only Finance users can complete payments.'); return; }
    if (!this.selectedExpenseId) { this.toast.showWarning('Please select an expense to pay.'); return; }
    if (!this.referenceNo.trim()) { this.toast.showWarning('Reference number is required.'); return; }

    this.loader.show();
    this.api.completePayment(this.selectedExpenseId, {
      referenceNo: this.referenceNo,
      paymentMode: this.paymentMode
    }).subscribe({
      next: () => {
        this.toast.show('Payment completed successfully 💰');
        this.resetForm();
        this.loadPayments();
        this.loadApprovedExpenses();
        this.loader.hide();
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Failed to complete payment. Expense must be Approved.');
        this.loader.hide();
      }
    });
  }

  // ================= LOAD ALL PAYMENTS =================
  loadPayments() {
    this.loading = true;
    this.loader.show();

    this.api.getAllPayments(1, 50).subscribe({
      next: (res: any) => {
        this.payments = res.data ?? res ?? [];
        this.loading  = false;
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load payments.');
        this.loading = false;
        this.loader.hide();
      }
    });
  }

  // ================= SEARCH PAYMENT =================
  searchPaymentByExpenseId() {
    if (!(this.isEmployee() || this.isManager())) {
      this.toast.showWarning('You are not allowed to search payments.'); return;
    }
    if (!this.searchExpenseId.trim()) {
      this.toast.showWarning('Please enter an Expense ID to search.'); return;
    }

    this.searching = true;
    this.loader.show();

    this.api.getPaymentByExpenseId(this.searchExpenseId.trim()).subscribe({
      next: (res: any) => {
        this.loader.hide();
        this.searching = false;

        const payment = res?.data ?? res;
        if (payment) {
          this.searchResults = [payment];
          this.toast.show('Payment found ✅');
        } else {
          this.searchResults = [];
          this.toast.showWarning('No payment found for this Expense ID.');
        }
      },
      error: (err) => {
        this.loader.hide();
        this.searching = false;
        this.searchResults = [];
        this.toast.showError(
          err?.status === 404 ? 'No payment found for this Expense ID.' : 'Failed to fetch payment.'
        );
      }
    });
  }

  // ================= VIEW PAYMENT =================
  viewPayment(expenseId: string) {
    this.loader.show();
    this.api.getPaymentByExpenseId(expenseId).subscribe({
      next: (res: any) => {
        this.loader.hide();
        const payment = res?.data ?? res;
        if (payment && payment.paymentStatus === 'Paid') {
          this.modalPaymentDetails = payment;
          this.showPaymentModal = true;
        } else if (payment) {
          this.toast.showWarning('Payment exists but is not completed yet.');
        } else {
          this.toast.showWarning('No payment record found for this expense.');
        }
      },
      error: () => {
        this.loader.hide();
        this.toast.showWarning('No payment found for this expense yet.');
      }
    });
  }

  closePaymentModal() {
    this.showPaymentModal = false;
    this.modalPaymentDetails = null;
  }

  // ================= RESET =================
  resetForm() {
    this.selectedExpenseId = '';
    this.referenceNo       = '';
    this.paymentMode       = 'BankTransfer';
  }
}