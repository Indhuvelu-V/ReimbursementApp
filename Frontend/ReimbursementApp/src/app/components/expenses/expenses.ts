// import { Component, OnInit, inject } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { FormBuilder, Validators, ReactiveFormsModule, FormGroup } from '@angular/forms';
// import { finalize } from 'rxjs';
// import { APIService } from '../../services/api.service';
// import { ToastService } from '../../services/toast.service';
// import { LoaderService } from '../../services/loader.service';
// import { TokenService } from '../../services/token.service';
// import { ExpenseStatusTracker } from '../expense-status-tracker/expense-status-tracker';

// @Component({
//   selector: 'app-expenses',
//   standalone: true,
//   imports: [CommonModule, ReactiveFormsModule, ExpenseStatusTracker],
//   templateUrl: './expenses.html',
//   styleUrls: ['./expenses.css']
// })
// export class Expenses implements OnInit {

//   private fb           = inject(FormBuilder);
//   private api          = inject(APIService);
//   private toast        = inject(ToastService);
//   private loader       = inject(LoaderService);
//   private tokenService = inject(TokenService);

//   expenses:             any[]        = [];
//   files:                File[]       = [];
//   isEditMode                         = false;
//   editExpenseId:        string | null = null;
//   lastCreatedExpenseId: string | null = null;
//   role:                 string        = '';

//   page     = 1;
//   pageSize = 6;
//   total    = 0;


//   filterStatus = '';

//   form!: FormGroup;

//   ngOnInit() {
//     this.role = this.tokenService.getRoleFromToken() ?? '';
//     this.form = this.fb.group({
//       categoryId:  ['', Validators.required],
//       amount:      ['', [Validators.required, Validators.min(0.01)]],
//       expenseDate: ['', Validators.required]
//     });
//     this.loadExpenses();
//   }


//   loadExpenses() {
//     this.loader.show();
//     this.api.getMyExpenses().subscribe({
//       next: (res) => {
//         let all: any[] = Array.isArray(res) ? res : (res.data ?? []);


//         if (this.filterStatus) {
//           all = all.filter((e: any) => e.status === this.filterStatus);
//         }

//         this.total    = all.length;

      
//         const start   = (this.page - 1) * this.pageSize;
//         this.expenses = all.slice(start, start + this.pageSize);
//         this.loader.hide();
//       },
//       error: () => {
//         this.toast.showError('Failed to load expenses. Please try again.');
//         this.loader.hide();
//       }
//     });
//   }


//   applyFilter(status: string) {
//     this.filterStatus = status;
//     this.page = 1;
//     this.loadExpenses();
//   }

//   clearFilter() {
//     this.filterStatus = '';
//     this.page = 1;
//     this.loadExpenses();
//   }

 
//   totalPages(): number {
//     return Math.ceil(this.total / this.pageSize);
//   }

//   nextPage() {
//     if (this.page < this.totalPages()) {
//       this.page++;
//       this.loadExpenses();
//     }
//   }

//   prevPage() {
//     if (this.page > 1) {
//       this.page--;
//       this.loadExpenses();
//     }
//   }


//   onFileChange(event: any) {
//     this.files = Array.from(event.target.files);
//   }


//   submit() {
//     this.form.markAllAsTouched();
//     if (this.form.invalid) {
//       this.toast.showWarning('Please fill in all required fields correctly.');
//       return;
//     }

//     const formData = new FormData();
//     formData.append('categoryId',  this.form.value.categoryId);
//     formData.append('amount',      this.form.value.amount);
//     formData.append('expenseDate', this.form.value.expenseDate);
//     this.files.forEach(file => formData.append('Documents', file));

//     if (this.isEditMode && this.editExpenseId) {
//       this.loader.show();
//       this.api.updateExpense(this.editExpenseId, formData)
//         .pipe(finalize(() => this.files = []))
//         .subscribe({
         
//           next: (res) => {
//             this.toast.show(
//               res?.message || 'Expense updated successfully ✅'
//             );
//             this.resetForm();
//             this.loadExpenses();
//           },
//           error: (err) => {
//             this.toast.showError(
//               err?.error?.message || 'Failed to update expense. Please try again.'
//             );
//             this.loader.hide();
//           }
//         });

  
//     } else {
//       this.loader.show();
//       this.api.createExpense(formData)
//         .pipe(finalize(() => this.files = []))
//         .subscribe({
//           next: (res) => {
          
//             const created = res?.expense ?? res;
//             this.lastCreatedExpenseId = created?.expenseId ?? null;

//             this.toast.show(
//               this.lastCreatedExpenseId
//                 ? `Expense created! ID: ${this.lastCreatedExpenseId} ✅`
//                 : 'Expense created successfully ✅'
//             );
//             this.resetForm();
//             this.loadExpenses();
//           },
//           error: (err) => {
//             this.toast.showError(
//               err?.error?.message || 'Failed to create expense. Please try again.'
//             );
//             this.loader.hide();
//           }
//         });
//     }
//   }


//   edit(expense: any) {
//     if (!expense.canEdit) {
//       this.toast.showWarning('This expense cannot be edited after manager approval.');
//       return;
//     }
//     this.isEditMode    = true;
//     this.editExpenseId = expense.expenseId;
//     this.form.patchValue({
//       categoryId:  expense.categoryId,
//       amount:      expense.amount,
//       expenseDate: this.formatDate(expense.expenseDate)
//     });
//     window.scrollTo({ top: 0, behavior: 'smooth' });
//   }

//   formatDate(dateStr: string): string {
//     if (!dateStr) return '';
//     const parts = dateStr.split('-');

//     if (parts.length === 3 && parts[0].length === 2) {
//       return `${parts[2]}-${parts[1]}-${parts[0]}`;
//     }
//     return dateStr;
//   }


//   delete(id: string) {
//     if (!confirm('Are you sure you want to delete this expense? This cannot be undone.')) return;

//     this.loader.show();
//     this.api.deleteExpense(id)
//       .pipe(finalize(() => {}))
//       .subscribe({
//         next: (res) => {
//           this.toast.show(res?.message || 'Expense deleted successfully ✅');
//           this.loadExpenses();
//         },
//         error: (err) => {
//           this.toast.showError(
//             err?.error?.message || 'Failed to delete. Only Draft/Submitted expenses can be deleted.'
//           );
//           this.loader.hide();
//         }
//       });
//   }

 
//   submitExpense(id: string) {
//     this.loader.show();
//     this.api.submitExpense(id)
//       .pipe(finalize(() => {}))
//       .subscribe({
//         next: () => {
//           this.toast.show('Expense submitted for manager approval ✅');
//           this.loadExpenses();
//         },
//         error: (err) => {
//           this.toast.showError(
//             err?.error?.message || 'Failed to submit expense.'
//           );
//           this.loader.hide();
//         }
//       });
//   }


//   resetForm() {
//     this.form.reset();
//     this.files         = [];
//     this.isEditMode    = false;
//     this.editExpenseId = null;
//   }

//   dismissExpenseId() {
//     this.lastCreatedExpenseId = null;
//   }


//   get categoryId() { return this.form.get('categoryId')!; }
//   get amount()     { return this.form.get('amount')!; }
//   get expenseDate(){ return this.form.get('expenseDate')!; }
// }
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormGroup, AbstractControl, ValidationErrors } from '@angular/forms';
import { finalize } from 'rxjs';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { LoaderService } from '../../services/loader.service';
import { TokenService } from '../../services/token.service';
import { ExpenseStatusTracker } from '../expense-status-tracker/expense-status-tracker';

// ─────────────────────────────────────────────────────────────────────────────
// Custom validator: selected date must be within the current calendar month.
// Matches the backend RULE 1 check exactly.
// ─────────────────────────────────────────────────────────────────────────────
function currentMonthOnlyValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;

  const selected   = new Date(control.value);           // yyyy-MM-dd from <input type="date">
  const today      = new Date();
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
  const monthEnd   = new Date(today.getFullYear(), today.getMonth() + 1, 0); // last day

  if (selected < monthStart || selected > monthEnd) {
    return { notCurrentMonth: true };
  }
  return null;
}

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ExpenseStatusTracker],
  templateUrl: './expenses.html',
  styleUrls: ['./expenses.css']
})
export class Expenses implements OnInit {

  private fb           = inject(FormBuilder);
  private api          = inject(APIService);
  private toast        = inject(ToastService);
  private loader       = inject(LoaderService);
  private tokenService = inject(TokenService);

  expenses:             any[]         = [];
  files:                File[]        = [];
  isEditMode                          = false;
  editExpenseId:        string | null = null;
  lastCreatedExpenseId: string | null = null;
  role:                 string        = '';

  // Pagination
  page     = 1;
  pageSize = 6;
  total    = 0;

  // Filter
  filterStatus = '';

  // Full unfiltered list — used for the one-expense-per-month duplicate check
  private allExpenses: any[] = [];

  // ✅ Date boundaries exposed to the template for [min] / [max] on the date input
  //    Locks the calendar picker to the current month only.
  readonly minDate: string;        // e.g.  "2025-03-01"
  readonly maxDate: string;        // e.g.  "2025-03-31"
  readonly currentMonthLabel: string; // e.g.  "March 2025"

  form!: FormGroup;

  constructor() {
    const today      = new Date();
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const monthEnd   = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    this.minDate           = this.toInputDate(monthStart);
    this.maxDate           = this.toInputDate(monthEnd);
    this.currentMonthLabel = today.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  ngOnInit() {
    this.role = this.tokenService.getRoleFromToken() ?? '';
    this.form = this.fb.group({
      categoryId:  ['', Validators.required],
      amount:      ['', [Validators.required, Validators.min(0.01)]],
      // ✅ currentMonthOnlyValidator enforces no past / future month dates
      expenseDate: ['', [Validators.required, currentMonthOnlyValidator]]
    });
    this.loadExpenses();
  }

  // ================= LOAD =================
  loadExpenses() {
    this.loader.show();
    this.api.getMyExpenses().subscribe({
      next: (res) => {
        const all: any[] = Array.isArray(res) ? res : (res.data ?? []);

        // ✅ Always keep the complete unfiltered list for duplicate checking
        this.allExpenses = all;

        let filtered = [...all];
        if (this.filterStatus) {
          filtered = filtered.filter((e: any) => e.status === this.filterStatus);
        }

        this.total    = filtered.length;
        const start   = (this.page - 1) * this.pageSize;
        this.expenses = filtered.slice(start, start + this.pageSize);
        this.loader.hide();
      },
      error: () => {
        this.toast.showError('Failed to load expenses. Please try again.');
        this.loader.hide();
      }
    });
  }

  // ================= FILTER =================
  applyFilter(status: string) {
    this.filterStatus = status;
    this.page = 1;
    this.loadExpenses();
  }

  clearFilter() {
    this.filterStatus = '';
    this.page = 1;
    this.loadExpenses();
  }

  // ================= PAGINATION =================
  totalPages(): number {
    return Math.ceil(this.total / this.pageSize);
  }

  nextPage() {
    if (this.page < this.totalPages()) { this.page++; this.loadExpenses(); }
  }

  prevPage() {
    if (this.page > 1) { this.page--; this.loadExpenses(); }
  }

  // ================= FILE =================
  onFileChange(event: any) {
    this.files = Array.from(event.target.files);
  }

  // ================= CREATE / UPDATE =================
  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      // Specific toast when only the date is wrong
      if (this.expenseDate.errors?.['notCurrentMonth']) {
        this.toast.showWarning(
          `Only expenses for ${this.currentMonthLabel} are allowed. ` +
          `Please choose a date within this month.`
        );
      } else {
        this.toast.showWarning('Please fill in all required fields correctly.');
      }
      return;
    }

    const formData = new FormData();
    formData.append('categoryId',  this.form.value.categoryId);
    formData.append('amount',      this.form.value.amount);
    formData.append('expenseDate', this.form.value.expenseDate);
    this.files.forEach(file => formData.append('Documents', file));

    // ===== UPDATE =====
    if (this.isEditMode && this.editExpenseId) {
      this.loader.show();
      this.api.updateExpense(this.editExpenseId, formData)
        .pipe(finalize(() => this.files = []))
        .subscribe({
          next: (res) => {
            this.toast.show(res?.message || 'Expense updated successfully ✅');
            this.resetForm();
            this.loadExpenses();
          },
          error: (err) => {
            this.toast.showError(
              err?.error?.message || 'Failed to update expense. Please try again.'
            );
            this.loader.hide();
          }
        });

    // ===== CREATE =====
    } else {

      const today      = new Date();
      const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
      const monthEnd   = new Date(today.getFullYear(), today.getMonth() + 1, 0);

      // ── CLIENT-SIDE GUARD 1: current month only ──────────────────────────
      // The form validator already catches this, but kept as a safety net
      // in case the form value is set programmatically.
      const selectedDate = new Date(this.form.value.expenseDate);
      if (selectedDate < monthStart || selectedDate > monthEnd) {
        this.toast.showWarning(
          `Only expenses for ${this.currentMonthLabel} are allowed. ` +
          `You cannot create expenses for past or future months.`
        );
        return;
      }
      // ─────────────────────────────────────────────────────────────────────

      // ── CLIENT-SIDE GUARD 2: one expense per current month ───────────────
      // Uses allExpenses (full unfiltered list) so pagination / filters
      // don't affect the check. Mirrors backend RULE 2 exactly.
      const duplicate = this.allExpenses.some((e: any) => {
        // Backend stores date as dd-MM-yyyy — parse it back
        const parts = (e.expenseDate ?? '').split('-');
        const eDate = (parts.length === 3 && parts[0].length === 2)
          ? new Date(`${parts[2]}-${parts[1]}-${parts[0]}`)
          : new Date(e.expenseDate);

        return (
          eDate >= monthStart  &&
          eDate <= monthEnd    &&
          e.status !== 'Rejected'  // Rejected expenses can be recreated
        );
      });

      if (duplicate) {
        this.toast.showWarning(
          `You already have an expense for ${this.currentMonthLabel}. ` +
          `Only one expense is allowed per month.`
        );
        return;
      }
      // ─────────────────────────────────────────────────────────────────────

      this.loader.show();
      this.api.createExpense(formData)
        .pipe(finalize(() => this.files = []))
        .subscribe({
          next: (res) => {
            const created = res?.expense ?? res;
            this.lastCreatedExpenseId = created?.expenseId ?? null;
            this.toast.show(
              this.lastCreatedExpenseId
                ? `Expense created! ID: ${this.lastCreatedExpenseId} ✅`
                : 'Expense created successfully ✅'
            );
            this.resetForm();
            this.loadExpenses();
          },
          error: (err) => {
            // Backend is the final enforcer — surface its exact message
            this.toast.showError(
              err?.error?.message || 'Failed to create expense. Please try again.'
            );
            this.loader.hide();
          }
        });
    }
  }

  // ================= EDIT =================
  edit(expense: any) {
    if (!expense.canEdit) {
      this.toast.showWarning('This expense cannot be edited after manager approval.');
      return;
    }
    this.isEditMode    = true;
    this.editExpenseId = expense.expenseId;
    this.form.patchValue({
      categoryId:  expense.categoryId,
      amount:      expense.amount,
      expenseDate: this.formatDate(expense.expenseDate)
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const parts = dateStr.split('-');
    // Backend returns dd-MM-yyyy → convert to yyyy-MM-dd for <input type="date">
    if (parts.length === 3 && parts[0].length === 2) {
      return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }
    return dateStr;
  }

  // Helper: Date → "yyyy-MM-dd" string for [min] / [max] bindings
  private toInputDate(d: Date): string {
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${mm}-${dd}`;
  }

  // ================= DELETE =================
  delete(id: string) {
    if (!confirm('Are you sure you want to delete this expense? This cannot be undone.')) return;

    this.loader.show();
    this.api.deleteExpense(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: (res) => {
          this.toast.show(res?.message || 'Expense deleted successfully ✅');
          this.loadExpenses();
        },
        error: (err) => {
          this.toast.showError(
            err?.error?.message || 'Failed to delete. Only Draft/Submitted expenses can be deleted.'
          );
          this.loader.hide();
        }
      });
  }

  // ================= SUBMIT EXPENSE =================
  submitExpense(id: string) {
    this.loader.show();
    this.api.submitExpense(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => {
          this.toast.show('Expense submitted for manager approval ✅');
          this.loadExpenses();
        },
        error: (err) => {
          this.toast.showError(
            err?.error?.message || 'Failed to submit expense.'
          );
          this.loader.hide();
        }
      });
  }

  // ================= RESET =================
  resetForm() {
    this.form.reset();
    this.files         = [];
    this.isEditMode    = false;
    this.editExpenseId = null;
  }

  dismissExpenseId() {
    this.lastCreatedExpenseId = null;
  }

  // ================= FORM GETTERS =================
  get categoryId() { return this.form.get('categoryId')!; }
  get amount()     { return this.form.get('amount')!; }
  get expenseDate(){ return this.form.get('expenseDate')!; }
}