import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TokenService } from '../../services/token.service';
import { ToastService } from '../../services/toast.service';
import { APIService } from '../../services/api.service';
import { LoaderService } from '../../services/loader.service';
import { CreateApprovalRequestDto } from '../../models/approval.model';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './approvals.html',
  styleUrls: ['./approvals.css']
})
export class Approvals implements OnInit {
  private fb      = inject(FormBuilder);
  private api     = inject(APIService);
  private token   = inject(TokenService);
  private toast   = inject(ToastService);
  private loader  = inject(LoaderService);

  approvals:        any[] = [];
  submittedExpenses: any[] = [];   // Req 10: Manager dropdown of Submitted expenses
  loading = false;
  role: string = '';

  approvalForm: FormGroup;

  constructor() {
    this.approvalForm = this.fb.group({
      expenseId: ['', Validators.required],
      status:    ['approved', Validators.required],
      comments:  ['']
    });
  }

  ngOnInit(): void {
    this.role = this.token.getRoleFromToken() ?? '';
    this.loadApprovals();

    // Manager: load submitted expenses for dropdown
    if (this.role.toLowerCase() === 'manager') {
      this.loadSubmittedExpenses();
    }
  }

  loadApprovals(): void {
    this.loading = true;

    if (this.role.toLowerCase() === 'admin') {
      this.loader.show();
      this.api.getAllApprovals({ pageNumber: 1, pageSize: 50 }).subscribe({
        next: (res) => {
          this.approvals = res.data ?? [];
          this.loading   = false;
          this.loader.hide();
        },
        error: () => {
          this.toast.showError('Failed to load approvals. Please try again.');
          this.loading = false;
          this.loader.hide();
        }
      });
    } else {
      this.approvals = [];
      this.loading   = false;
    }
  }

  /** Req 10: Dropdown — fetch all Submitted expenses for Manager */
  loadSubmittedExpenses(): void {
    this.api.getAllExpenses(1, 100).subscribe({
      next: (res) => {
        const all = res.data ?? res ?? [];
        this.submittedExpenses = all.filter((e: any) => e.status === 'Submitted');
      },
      error: () => {} // silent — dropdown enhancement only
    });
  }

  approve(): void {
    if (this.approvalForm.invalid) {
      this.toast.showWarning('Please fill in all required fields.');
      return;
    }

    const managerId = this.token.getUserIdFromToken();
    if (!managerId) {
      this.toast.showError('User ID not found in token. Please log in again.');
      return;
    }

    const r = this.role.toLowerCase();

    if (r === 'manager') {
      const request: CreateApprovalRequestDto = {
        expenseId: this.approvalForm.value.expenseId,
        managerId,
        status:   this.approvalForm.value.status,
        comments: this.approvalForm.value.comments,
        level:    'Manager'
      };

      this.loader.show();
      this.api.managerApproval(request).subscribe({
        next: (res) => {
          const decision = request.status === 'approved' ? 'Approved ✅' : 'Rejected ❌';
          this.toast.show(`Expense ${decision} successfully`);
          this.approvalForm.reset({ status: 'approved' });
          this.loadSubmittedExpenses();
          this.loader.hide();
        },
        error: (err) => {
          this.toast.showError(
            err?.error?.message || 'Approval failed. Expense must be in Submitted state.'
          );
          this.loader.hide();
        }
      });

    } else {
      this.toast.showError('Unauthorized action. Only Manager can approve expenses.');
    }
  }
}
