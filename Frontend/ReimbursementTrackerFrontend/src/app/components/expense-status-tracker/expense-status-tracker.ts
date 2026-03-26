import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

const STATUS_STEP: Record<string, number> = {
  'Draft':     0,
  'Submitted': 1,
  'Approved':  2,
  'Paid':      3,
  'Rejected':  -1,
};

export interface TrackerStep { label: string; icon: string; }

@Component({
  selector: 'app-expense-status-tracker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './expense-status-tracker.html',
  styleUrls: ['./expense-status-tracker.css']
})
export class ExpenseStatusTracker {
  @Input() status: string = 'Draft';

  steps: TrackerStep[] = [
    { label: 'Created',          icon: 'bi-file-earmark-plus' },
    { label: 'Submitted',        icon: 'bi-send'               },
    { label: 'Manager Approved', icon: 'bi-person-check'       },
    { label: 'Finance Paid',     icon: 'bi-currency-rupee'     },
  ];

  get currentStep(): number  { return STATUS_STEP[this.status] ?? 0; }
  get isRejected(): boolean  { return this.status === 'Rejected'; }

  isCompleted(i: number): boolean     { return !this.isRejected && i < this.currentStep; }
  isActive(i: number): boolean        { return !this.isRejected && i === this.currentStep; }
  isPending(i: number): boolean       { return this.isRejected || i > this.currentStep; }
  isConnectorDone(i: number): boolean { return !this.isRejected && i + 1 <= this.currentStep; }
}
