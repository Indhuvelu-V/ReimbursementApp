import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { FileViewModal } from '../file-view-modal/file-view-modal';

@Component({
  selector: 'app-approval-history',
  standalone: true,
  imports: [CommonModule, FormsModule, FileViewModal],
  templateUrl: './approval-history.html',
  styleUrls: ['./approval-history.css']
})
export class ApprovalHistory implements OnInit {
  private api    = inject(APIService);
  private toast  = inject(ToastService);
  private loader = inject(LoaderService);

  approvals: any[] = [];
  filtered: any[]  = [];
  paged: any[]     = [];
  loading = false;

  filterStatus   = '';
  filterDateFrom = '';
  filterDateTo   = '';
  filterMinAmount: number | null = null;
  filterMaxAmount: number | null = null;
  filterUserName = '';       // expense submitter
  filterApproverName = '';   // manager who approved
  sortBy:  'amount' | 'date' | '' = '';
  sortDir: 'asc' | 'desc' = 'desc';

  page = 1; pageSize = 6;

  showFileModal = false; modalFileUrls: string[] = [];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.loader.show();
    this.api.getAllApprovals({ pageNumber: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.approvals = res.data ?? [];
        this.applyFilters();
        this.loading = false;
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load approval history.');
        this.loading = false;
        this.loader.hide();
      }
    });
  }

  applyFilters(): void {
    let data = [...this.approvals];
    if (this.filterStatus)
      data = data.filter(a => a.status === this.filterStatus);
    if (this.filterUserName)
      data = data.filter(a =>
        (a.employeeName ?? '').toLowerCase().includes(this.filterUserName.toLowerCase()) ||
        (a.expenseId ?? '').toLowerCase().includes(this.filterUserName.toLowerCase())
      );
    if (this.filterApproverName)
      data = data.filter(a =>
        (a.approverName ?? '').toLowerCase().includes(this.filterApproverName.toLowerCase())
      );
    if (this.filterMinAmount !== null)
      data = data.filter(a => Number(a.expenseAmount) >= Number(this.filterMinAmount));
    if (this.filterMaxAmount !== null)
      data = data.filter(a => Number(a.expenseAmount) <= Number(this.filterMaxAmount));
    if (this.filterDateFrom)
      data = data.filter(a => new Date(a.approvedAt) >= new Date(this.filterDateFrom));
    if (this.filterDateTo)
      data = data.filter(a => new Date(a.approvedAt) <= new Date(this.filterDateTo + 'T23:59:59'));
    if (this.sortBy === 'amount')
      data.sort((a, b) => this.sortDir === 'asc' ? Number(a.expenseAmount) - Number(b.expenseAmount) : Number(b.expenseAmount) - Number(a.expenseAmount));
    if (this.sortBy === 'date')
      data.sort((a, b) => this.sortDir === 'asc' ? new Date(a.approvedAt).getTime() - new Date(b.approvedAt).getTime() : new Date(b.approvedAt).getTime() - new Date(a.approvedAt).getTime());
    this.filtered = data;
    this.page = 1;
    this.updatePage();
  }

  updatePage() { const s = (this.page - 1) * this.pageSize; this.paged = this.filtered.slice(s, s + this.pageSize); }
  totalPages() { return Math.ceil(this.filtered.length / this.pageSize); }
  nextPage()   { if (this.page < this.totalPages()) { this.page++; this.updatePage(); } }
  prevPage()   { if (this.page > 1) { this.page--; this.updatePage(); } }

  toggleSort(field: 'amount' | 'date') {
    if (this.sortBy === field) { this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc'; }
    else { this.sortBy = field; this.sortDir = 'desc'; }
    this.applyFilters();
  }

  clearFilters() {
    this.filterStatus = ''; this.filterDateFrom = ''; this.filterDateTo = '';
    this.filterMinAmount = null; this.filterMaxAmount = null;
    this.filterUserName = ''; this.filterApproverName = '';
    this.sortBy = ''; this.sortDir = 'desc';
    this.applyFilters();
  }

  openFileModal(urls: string[]) { this.modalFileUrls = urls || []; this.showFileModal = true; }
  closeFileModal() { this.showFileModal = false; this.modalFileUrls = []; }
}
