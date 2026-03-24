import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

// Maps backend status string to tracker step index
const STATUS_STEP: Record<string, number> = {
  'Draft':     0,
  'Submitted': 1,
  'Approved':  2,
  'Paid':      3,
  'Rejected':  -1,
};

export interface TrackerStep {
  label: string;
  icon:  string;
}

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

  get currentStep(): number {
    return STATUS_STEP[this.status] ?? 0;
  }

  get isRejected(): boolean {
    return this.status === 'Rejected';
  }

  /** completed = strictly before current step */
  isCompleted(index: number): boolean {
    return !this.isRejected && index < this.currentStep;
  }

  /** active = exactly current step */
  isActive(index: number): boolean {
    return !this.isRejected && index === this.currentStep;
  }

  /** pending = after current step */
  isPending(index: number): boolean {
    return this.isRejected || index > this.currentStep;
  }

  /** connector between step i and i+1 should be green if step i+1 is completed or active */
  isConnectorDone(index: number): boolean {
    return !this.isRejected && index + 1 <= this.currentStep;
  }
}
