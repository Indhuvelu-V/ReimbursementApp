import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormGroup, AbstractControl, ValidationErrors } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { TokenService } from '../../services/token.service';
import { ExpenseStatusTracker } from '../expense-status-tracker/expense-status-tracker';
import { FileViewModal } from '../file-view-modal/file-view-modal';
import { CreateExpenseCategoryResponseDto } from '../../models/expensecategory.model';

function currentMonthOnlyValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  const today    = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
  const monthEnd   = new Date(today.getFullYear(), today.getMonth() + 1, 0);
  return (selected < monthStart || selected > monthEnd) ? { notCurrentMonth: true } : null;
}

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ExpenseStatusTracker, FileViewModal, CurrencyPipe],
  templateUrl: './expenses.html',
  styleUrls: ['./expenses.css']
})
export class Expenses implements OnInit {
  private fb           = inject(FormBuilder);
  private api          = inject(APIService);
  private toast        = inject(ToastService);
  private loader       = inject(LoaderService);
  private tokenService = inject(TokenService);

  // ── Tab state ──────────────────────────────────────────────────────────────
  activeTab: 'create' | 'list' = 'create';
  switchTab(tab: 'create' | 'list') {
    this.activeTab = tab;
    if (tab === 'list' && !this.allExpenses.length) this.loadExpenses();
  }

  // ── Data ───────────────────────────────────────────────────────────────────
  allExpenses: any[] = [];
  expenses:    any[] = [];      // current page slice
  pagedExpenses: any[] = [];    // alias used in template
  categories:  CreateExpenseCategoryResponseDto[] = [];

  // ── Form state ─────────────────────────────────────────────────────────────
  files: File[] = [];
  filePreviews: { url: string; name: string; isImage: boolean }[] = [];
  isEditMode      = false;
  editExpenseId:  string | null = null;
  lastCreatedExpenseId: string | null = null;
  role = '';
  approverName = ''; // reporting manager (employee) or admin name (manager/finance)

  // ── Filters ────────────────────────────────────────────────────────────────
  filterStatus      = '';
  filterCategory    = '';
  filterDateFrom    = '';
  filterDateTo      = '';
  filterMinAmount:  number | null = null;
  filterMaxAmount:  number | null = null;

  // ── Sort ───────────────────────────────────────────────────────────────────
  sortBy:  'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  // ── Pagination ─────────────────────────────────────────────────────────────
  page     = 1;
  pageSize = 6;
  total    = 0;

  // ── Category limit ─────────────────────────────────────────────────────────
  categoryMaxLimit:    number | null = null;
  categoryLimitName    = '';
  loadingCategoryLimit = false;

  // ── File modal ─────────────────────────────────────────────────────────────
  showFileModal  = false;
  modalFileUrls: string[] = [];
  existingDocUrls: string[] = [];   // files already saved on the expense being edited

  removeExistingFile(url: string) {
    this.existingDocUrls = this.existingDocUrls.filter(u => u !== url);
  }

  // ── Form dates ─────────────────────────────────────────────────────────────
  readonly minDate:          string;
  readonly maxDate:          string;
  readonly currentMonthLabel: string;
  form!: FormGroup;

  constructor(public apiSvc: APIService) {
    const today = new Date();
    const ms = new Date(today.getFullYear(), today.getMonth(), 1);
    const me = new Date(today.getFullYear(), today.getMonth() + 1, 0);
    this.minDate           = this.toInputDate(ms);
    this.maxDate           = this.toInputDate(today);  // today — no future dates
    this.currentMonthLabel = today.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';

    // Load approver name for toast message
    const r = this.role.toLowerCase();
    const myId = this.tokenService.getUserIdFromToken();
    if (r === 'employee' && myId) {
      // Employee → fetch own profile to get reporting manager name
      this.api.getUserById(myId).subscribe({
        next: (user: any) => { this.approverName = user?.reportingManagerName ?? ''; },
        error: () => {}
      });
    } else if (r === 'manager' || r === 'finance') {
      this.approverName = 'Admin';
    }

    this.form = this.fb.group({
      categoryId:  ['', Validators.required],
      amount:      ['', [Validators.required, Validators.min(0.01)]],
      expenseDate: ['', [Validators.required, currentMonthOnlyValidator]]
    });

    // Auto-fetch category limit when categoryId changes
    this.form.get('categoryId')!.valueChanges.subscribe(val => {
      if (val?.trim()) {
        this.fetchCategoryLimit(val.trim());
      } else {
        this.categoryMaxLimit = null;
        this.categoryLimitName = '';
      }
    });

    this.loadCategories();
    this.loadExpenses();
  }

  // ── Data loading ───────────────────────────────────────────────────────────

  loadCategories() {
    this.api.apiGetAllCategories().subscribe({
      next: (cats) => { this.categories = cats ?? []; },
      error: () => {}
    });
  }

  fetchCategoryLimit(categoryId: string) {
    const cat = this.categories.find(c => c.categoryId === categoryId);
    if (cat) {
      this.categoryMaxLimit  = cat.maxLimit ?? null;
      this.categoryLimitName = cat.categoryName ?? categoryId;
    } else {
      this.categoryMaxLimit  = null;
      this.categoryLimitName = '';
    }
  }

  loadExpenses() {
    this.loader.show();
    this.api.getMyExpenses().subscribe({
      next: (res) => {
        this.allExpenses = Array.isArray(res) ? res : (res.data ?? []);
        this.applyFiltersAndSort();
        this.loader.hide();
      },
      error: () => { this.toast.showError('Failed to load expenses.'); this.loader.hide(); }
    });
  }

  // ── Filter + Sort + Paginate (all client-side) ─────────────────────────────

  applyFiltersAndSort() {
    let data = [...this.allExpenses];

    // ── Status ──────────────────────────────────────────────────────────────
    if (this.filterStatus)
      data = data.filter(e => e.status === this.filterStatus);

    // ── Category ────────────────────────────────────────────────────────────
    if (this.filterCategory)
      data = data.filter(e =>
        (e.categoryName ?? '').toLowerCase() === this.filterCategory.toLowerCase()
      );

    // ── Amount range ────────────────────────────────────────────────────────
    if (this.filterMinAmount !== null)
      data = data.filter(e => e.amount >= this.filterMinAmount!);
    if (this.filterMaxAmount !== null)
      data = data.filter(e => e.amount <= this.filterMaxAmount!);

    // ── Date range ──────────────────────────────────────────────────────────
    if (this.filterDateFrom)
      data = data.filter(e =>
        new Date(this.parseDate(e.expenseDate)) >= new Date(this.filterDateFrom)
      );
    if (this.filterDateTo)
      data = data.filter(e =>
        new Date(this.parseDate(e.expenseDate)) <= new Date(this.filterDateTo + 'T23:59:59')
      );

    // ── Sort ─────────────────────────────────────────────────────────────────
    if (this.sortBy === 'amount') {
      data.sort((a, b) =>
        this.sortDir === 'asc' ? a.amount - b.amount : b.amount - a.amount
      );
    } else if (this.sortBy === 'date') {
      data.sort((a, b) => {
        const da = new Date(this.parseDate(a.expenseDate)).getTime();
        const db = new Date(this.parseDate(b.expenseDate)).getTime();
        return this.sortDir === 'asc' ? da - db : db - da;
      });
    }

    this.total = data.length;

    // ── Paginate ─────────────────────────────────────────────────────────────
    const start = (this.page - 1) * this.pageSize;
    this.expenses     = data.slice(start, start + this.pageSize);
    this.pagedExpenses = this.expenses;  // keep alias in sync
  }

  // ── Filter actions ─────────────────────────────────────────────────────────

  /** Quick status pill click */
  applyFilter(status: string) {
    this.filterStatus = status;
    this.page = 1;
    this.applyFiltersAndSort();
  }

  /** Apply the full filter panel */
  applyAdvancedFilters() {
    this.page = 1;
    this.applyFiltersAndSort();
  }

  /** Reset everything */
  clearFilters() {
    this.filterStatus   = '';
    this.filterCategory = '';
    this.filterDateFrom = '';
    this.filterDateTo   = '';
    this.filterMinAmount = null;
    this.filterMaxAmount = null;
    this.sortBy  = '';
    this.sortDir = 'desc';
    this.page    = 1;
    this.applyFiltersAndSort();
  }

  // ── Sort toggle (same pattern as Payments) ─────────────────────────────────

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy  = field;
      this.sortDir = 'desc';
    }
    this.applyFiltersAndSort();
  }

  // ── Pagination ─────────────────────────────────────────────────────────────

  totalPages(): number { return Math.ceil(this.total / this.pageSize); }
  nextPage()  { if (this.page < this.totalPages()) { this.page++; this.applyFiltersAndSort(); } }
  prevPage()  { if (this.page > 1) { this.page--; this.applyFiltersAndSort(); } }

  // ── File handling ──────────────────────────────────────────────────────────

  onFileChange(event: any) {
    const selected: File[] = Array.from(event.target.files ?? []);
    if (!selected.length) return;

    // Append new files to existing selection instead of replacing
    this.files = [...this.files, ...selected];
    this.filePreviews = this.files.map(f => ({
      url:     f.type.startsWith('image/') ? URL.createObjectURL(f) : '',
      name:    f.name,
      isImage: f.type.startsWith('image/')
    }));
    event.target.value = '';
  }

  removeFile(i: number) {
    this.files.splice(i, 1);
    this.filePreviews.splice(i, 1);
  }

  // ── Form submit ────────────────────────────────────────────────────────────

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.toast.showWarning(
        this.expenseDate.errors?.['notCurrentMonth']
          ? `Only expenses for ${this.currentMonthLabel} are allowed.`
          : 'Please fill in all required fields correctly.'
      );
      return;
    }

    // Category limit validation
    const entered = Number(this.form.value.amount);
    if (this.categoryMaxLimit !== null && entered > this.categoryMaxLimit) {
      const fmt = new Intl.NumberFormat('en-IN', {
        style: 'currency', currency: 'INR', maximumFractionDigits: 0
      }).format(this.categoryMaxLimit);
      this.toast.showError(
        `Amount exceeds maximum limit of ${fmt} for ${this.categoryLimitName} category`
      );
      return;
    }

    const fd = new FormData();
    fd.append('categoryId',   this.form.value.categoryId);
    // Also send categoryName so backend can use it directly
    const selectedCat = this.categories.find(c => c.categoryId === this.form.value.categoryId);
    fd.append('categoryName', selectedCat?.categoryName ?? '');
    fd.append('amount',      this.form.value.amount);
    fd.append('expenseDate', this.form.value.expenseDate);
    this.files.forEach(f => fd.append('Documents', f));

    if (this.isEditMode && this.editExpenseId) {
      // Send kept existing files so backend knows which ones to preserve
      console.log('[Edit Submit] existingDocUrls to keep:', this.existingDocUrls);
      console.log('[Edit Submit] new files:', this.files.map(f => f.name));
      // Always send DocumentUrls so backend knows the list is intentional.
      // If all files deleted, send sentinel '__EMPTY__' so backend doesn't fall back to old docs.
      if (this.existingDocUrls.length > 0) {
        this.existingDocUrls.forEach(u => fd.append('DocumentUrls', u));
      } else {
        fd.append('DocumentUrls', '__EMPTY__');
      }
      this.loader.show();
      this.api.updateExpense(this.editExpenseId, fd)
        .pipe(finalize(() => this.clearFiles()))
        .subscribe({
          next:  (res) => { this.toast.show(res?.message || 'Expense updated ✅'); this.resetForm(); this.loadExpenses(); },
          error: (err) => { this.toast.showError(err?.error?.message || 'Failed to update expense.'); this.loader.hide(); }
        });
    } else {
      const today      = new Date();
      const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
      const monthEnd   = new Date(today.getFullYear(), today.getMonth() + 1, 0);
      const selDate    = new Date(this.form.value.expenseDate);

      if (selDate < monthStart || selDate > monthEnd) {
        this.toast.showWarning(`Only expenses for ${this.currentMonthLabel} are allowed.`);
        return;
      }

      const duplicate = this.allExpenses.some(e => {
        const eDate = new Date(this.parseDate(e.expenseDate));
        return eDate >= monthStart && eDate <= monthEnd && e.status !== 'Rejected';
      });

      
      if (duplicate) {
        this.toast.showWarning(`You already have an expense for ${this.currentMonthLabel}.`);
        return;
      }

      this.loader.show();
      this.api.createExpense(fd)
        .pipe(finalize(() => this.clearFiles()))
        .subscribe({
          next: (res) => {
            const created = res?.expense ?? res;
            this.lastCreatedExpenseId = created?.expenseId ?? null;
            this.toast.show(
              this.lastCreatedExpenseId
                ? `Expense saved as Draft (ID: ${this.lastCreatedExpenseId}). Submit it for approval${this.approverName ? ' to ' + this.approverName : ''}. ✅`
                : `Expense saved as Draft. Submit it for approval${this.approverName ? ' to ' + this.approverName : ''}. ✅`
            );
            this.resetForm();
            this.loadExpenses();
          },
          error: (err) => { this.toast.showError(err?.error?.message || 'Failed to create expense.'); this.loader.hide(); }
        });
    }
  }

  // ── Expense actions ────────────────────────────────────────────────────────

  edit(expense: any) {
    if (!expense.canEdit) { this.toast.showWarning('This expense cannot be edited.'); return; }
    this.isEditMode    = true;
    this.editExpenseId = expense.expenseId;
    this.form.patchValue({
      categoryId:  expense.categoryId,
      amount:      expense.amount,
      expenseDate: this.formatDate(expense.expenseDate)
    });

    // Show existing uploaded files as previews (read-only, not re-uploaded)
    this.existingDocUrls = expense.documentUrls ?? [];

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async delete(id: string) {
    const ok = await this.toast.confirm(
      'Delete Expense',
      `Are you sure you want to delete expense ${id}? This cannot be undone.`
    );
    if (!ok) return;
    this.loader.show();
    this.api.deleteExpense(id).subscribe({
      next:  (res) => { this.toast.show(res?.message || 'Expense deleted ✅'); this.loadExpenses(); },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to delete.'); this.loader.hide(); }
    });
  }

  submitExpense(id: string) {
    this.loader.show();
    this.api.submitExpense(id).subscribe({
      next: (res: any) => {
        const r = this.role.toLowerCase();
        let msg: string;
        if (r === 'admin') {
          msg = 'Expense auto-approved and sent for payment ✅';
        } else {
          msg = this.approverName
            ? `Expense submitted for approval to ${this.approverName} ✅`
            : 'Expense submitted for approval ✅';
        }
        this.toast.show(msg);
        this.loadExpenses();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to submit.'); this.loader.hide(); }
    });
  }

  resubmit(id: string) {
    this.loader.show();
    this.api.resubmitExpense(id).subscribe({
      next: () => {
        const msg = this.approverName
          ? `Expense resubmitted for approval to ${this.approverName} ✅`
          : 'Expense resubmitted ✅';
        this.toast.show(msg);
        this.loadExpenses();
      },
      error: (err) => { this.toast.showError(err?.error?.message || 'Failed to resubmit.'); this.loader.hide(); }
    });
  }

  // ── File modal ─────────────────────────────────────────────────────────────

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal()               { this.showFileModal = false; this.modalFileUrls = []; }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private parseDate(dateStr: string): string {
    if (!dateStr) return '';
    const p = dateStr.split('-');
    return (p.length === 3 && p[0].length === 2) ? `${p[2]}-${p[1]}-${p[0]}` : dateStr;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const p = dateStr.split('-');
    return (p.length === 3 && p[0].length === 2) ? `${p[2]}-${p[1]}-${p[0]}` : dateStr;
  }

  private toInputDate(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
  }

  clearFiles()  { this.files = []; this.filePreviews = []; }
  resetForm()   {
    this.form.reset();
    this.clearFiles();
    this.isEditMode      = false;
    this.editExpenseId   = null;
    this.existingDocUrls = [];
    this.categoryMaxLimit  = null;
    this.categoryLimitName = '';
  }
  dismissExpenseId() { this.lastCreatedExpenseId = null; }

  // ── Computed helpers for template ──────────────────────────────────────────

  /** Count expenses by status from the full unfiltered list */
  countByStatus(status: string): number {
    return this.allExpenses.filter(e => e.status === status).length;
  }

  /** True when any filter / sort is active */
  get hasActiveFilters(): boolean {
    return !!(this.filterStatus || this.filterCategory || this.filterDateFrom ||
              this.filterDateTo || this.filterMinAmount || this.filterMaxAmount || this.sortBy);
  }

  get categoryId()  { return this.form.get('categoryId')!; }
  get amount()      { return this.form.get('amount')!; }
  get expenseDate() { return this.form.get('expenseDate')!; }
}
