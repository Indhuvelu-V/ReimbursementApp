import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
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
  imports: [ReactiveFormsModule, CommonModule, FormsModule, FileViewModal],
  templateUrl: './approvals.html',
  styleUrls: ['./approvals.css']
})
export class Approvals implements OnInit {
  private fb     = inject(FormBuilder);
  private api    = inject(APIService);
  private token  = inject(TokenService);
  private toast  = inject(ToastService);
  private loader = inject(LoaderService);

  approvals: any[] = [];
  filteredApprovals: any[] = [];
  pagedApprovals: any[] = [];
  submittedExpenses: any[] = [];
  loading = false;
  role: string = '';

  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  sortBy: 'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  page = 1; pageSize = 6;

  showFileModal = false; modalFileUrls: string[] = [];

  approvalForm!: FormGroup;

  constructor(public apiSvc: APIService) {}

  ngOnInit(): void {
    this.approvalForm = this.fb.group({
      expenseId: ['', Validators.required],
      status:    ['approved', Validators.required],
      comments:  ['']
    });
    this.role = this.token.getRoleFromToken() ?? '';
    this.loadApprovals();
    if (this.role.toLowerCase() === 'manager') this.loadSubmittedExpenses();
  }

  loadApprovals(): void {
    this.loading = true;
    if (this.role.toLowerCase() === 'admin') {
      this.loader.show();
      this.api.getAllApprovals({ pageNumber: 1, pageSize: 200 }).subscribe({
        next: (res) => { this.approvals = res.data ?? []; this.applyFilters(); this.loading = false; this.loader.hide(); },
        error: () => { this.toast.showError('Failed to load approvals.'); this.loading = false; this.loader.hide(); }
      });
    } else {
      this.approvals = []; this.loading = false;
    }
  }

  loadSubmittedExpenses(): void {
    this.api.getAllExpenses(1, 200).subscribe({
      next: (res) => {
        const all = res.data ?? res ?? [];
        this.submittedExpenses = all.filter((e: any) => e.status === 'Submitted');
      },
      error: () => {}
    });
  }

  applyFilters() {
    let data = [...this.approvals];
    if (this.filterStatus) data = data.filter(a => a.status === this.filterStatus);
    if (this.filterMinAmount !== null) data = data.filter(a => a.expenseAmount >= this.filterMinAmount!);
    if (this.filterMaxAmount !== null) data = data.filter(a => a.expenseAmount <= this.filterMaxAmount!);
    if (this.filterDateFrom) data = data.filter(a => new Date(a.approvedAt) >= new Date(this.filterDateFrom));
    if (this.filterDateTo)   data = data.filter(a => new Date(a.approvedAt) <= new Date(this.filterDateTo + 'T23:59:59'));
    if (this.sortBy === 'amount') data.sort((a, b) => this.sortDir === 'asc' ? (a.expenseAmount - b.expenseAmount) : (b.expenseAmount - a.expenseAmount));
    if (this.sortBy === 'date')   data.sort((a, b) => this.sortDir === 'asc' ? new Date(a.approvedAt).getTime() - new Date(b.approvedAt).getTime() : new Date(b.approvedAt).getTime() - new Date(a.approvedAt).getTime());
    this.filteredApprovals = data;
    this.page = 1;
    this.updatePage();
  }

  updatePage() { const s = (this.page - 1) * this.pageSize; this.pagedApprovals = this.filteredApprovals.slice(s, s + this.pageSize); }
  totalPages() { return Math.ceil(this.filteredApprovals.length / this.pageSize); }
  nextPage() { if (this.page < this.totalPages()) { this.page++; this.updatePage(); } }
  prevPage() { if (this.page > 1) { this.page--; this.updatePage(); } }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) { this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.applyFilters();
  }

  clearFilters() { this.filterStatus = ''; this.filterDateFrom = ''; this.filterDateTo = ''; this.filterMinAmount = null; this.filterMaxAmount = null; this.sortBy = ''; this.sortDir = 'desc'; this.applyFilters(); }

  approve(): void {
    if (this.approvalForm.invalid) { this.toast.showWarning('Please fill in all required fields.'); return; }
    const managerId = this.token.getUserIdFromToken();
    if (!managerId) { this.toast.showError('User ID not found in token.'); return; }
    if (this.role.toLowerCase() === 'manager') {
      const request: CreateApprovalRequestDto = {
        expenseId: this.approvalForm.value.expenseId,
        managerId,
        status:   this.approvalForm.value.status,
        comments: this.approvalForm.value.comments,
        level:    'Manager'
      };
      this.loader.show();
      this.api.managerApproval(request).subscribe({
        next: () => {
          this.toast.show(`Expense ${request.status === 'approved' ? 'Approved ✅' : 'Rejected ❌'} successfully`);
          this.approvalForm.reset({ status: 'approved' });
          this.loadSubmittedExpenses();
          this.loader.hide();
        },
        error: (err) => { this.toast.showError(err?.error?.message || 'Approval failed.'); this.loader.hide(); }
      });
    } else {
      this.toast.showError('Only Manager can approve expenses.');
    }
  }

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
