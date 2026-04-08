import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  {
    path: 'login',
    loadComponent: () => import('./login/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./user-register-component/user-register-component').then(m => m.UserRegisterComponent)
  },

  // ── Employee ──────────────────────────────────────────────────────────────
  {
    path: 'employee',
    loadComponent: () => import('./employee-dashboard/employee-dashboard').then(m => m.EmployeeDashboard),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            loadComponent: () => import('./components/expenses/expenses').then(m => m.Expenses) },
      { path: 'policy',             loadComponent: () => import('./components/policies/policies').then(m => m.Policies) },
      { path: 'notifications',      loadComponent: () => import('./components/notifications/notifications').then(m => m.Notifications) },
      { path: 'expense-categories', loadComponent: () => import('./components/expense-categories/expense-categories').then(m => m.ExpenseCategories) },
      { path: 'payment',            loadComponent: () => import('./components/payments/payments').then(m => m.Payments) },
      { path: 'employee-data',      loadComponent: () => import('./components/users-datas/employee-datas').then(m => m.EmployeeDatas) },
    ]
  },

  // ── Team Lead ─────────────────────────────────────────────────────────────
  {
    path: 'teamlead',
    loadComponent: () => import('./teamlead-dashboard/teamlead-dashboard').then(m => m.TeamLeadDashboard),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            loadComponent: () => import('./components/expenses/expenses').then(m => m.Expenses) },
      { path: 'approvals',          loadComponent: () => import('./components/teamlead-approvals/teamlead-approvals').then(m => m.TeamLeadApprovals) },
      { path: 'policy',             loadComponent: () => import('./components/policies/policies').then(m => m.Policies) },
      { path: 'expense-categories', loadComponent: () => import('./components/expense-categories/expense-categories').then(m => m.ExpenseCategories) },
      { path: 'notifications',      loadComponent: () => import('./components/notifications/notifications').then(m => m.Notifications) },
    ]
  },

  // ── Manager ───────────────────────────────────────────────────────────────
  {
    path: 'manager',
    loadComponent: () => import('./manager-dashboard/manager-dashboard').then(m => m.ManagerDashboard),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            loadComponent: () => import('./components/expenses/expenses').then(m => m.Expenses) },
      { path: 'policy',             loadComponent: () => import('./components/policies/policies').then(m => m.Policies) },
      { path: 'approvals',          loadComponent: () => import('./components/approvals/approvals').then(m => m.Approvals) },
      { path: 'approval-history',   loadComponent: () => import('./components/approval-history/approval-history').then(m => m.ApprovalHistory) },
      { path: 'notifications',      loadComponent: () => import('./components/notifications/notifications').then(m => m.Notifications) },
      { path: 'expense-categories', loadComponent: () => import('./components/expense-categories/expense-categories').then(m => m.ExpenseCategories) },
      { path: 'payment',            loadComponent: () => import('./components/payments/payments').then(m => m.Payments) },
      { path: 'employee-data',      loadComponent: () => import('./components/users-datas/employee-datas').then(m => m.EmployeeDatas) },
    ]
  },

  // ── Finance ───────────────────────────────────────────────────────────────
  {
    path: 'finance',
    loadComponent: () => import('./finance-dashboard/finance-dashboard').then(m => m.FinanceDashboard),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'payment', pathMatch: 'full' },
      { path: 'expense',            loadComponent: () => import('./components/expenses/expenses').then(m => m.Expenses) },
      { path: 'expense-categories', loadComponent: () => import('./components/expense-categories/expense-categories').then(m => m.ExpenseCategories) },
      { path: 'policy',             loadComponent: () => import('./components/policies/policies').then(m => m.Policies) },
      { path: 'log',                loadComponent: () => import('./components/logs/logs').then(m => m.Logs) },
      { path: 'payment',            loadComponent: () => import('./components/payments/payments').then(m => m.Payments) },
      { path: 'employee-data',      loadComponent: () => import('./components/users-datas/employee-datas').then(m => m.EmployeeDatas) },
      { path: 'notifications',      loadComponent: () => import('./components/notifications/notifications').then(m => m.Notifications) },
    ]
  },

  // ── Admin ─────────────────────────────────────────────────────────────────
  {
    path: 'admin',
    loadComponent: () => import('./admin-dashboard/admin-dashboard').then(m => m.AdminDashboard),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            loadComponent: () => import('./components/expenses/expenses').then(m => m.Expenses) },
      { path: 'expense-categories', loadComponent: () => import('./components/expense-categories/expense-categories').then(m => m.ExpenseCategories) },
      { path: 'policy',             loadComponent: () => import('./components/policies/policies').then(m => m.Policies) },
      { path: 'log',                loadComponent: () => import('./components/logs/logs').then(m => m.Logs) },
      { path: 'payment',            loadComponent: () => import('./components/payments/payments').then(m => m.Payments) },
      { path: 'employee-data',      loadComponent: () => import('./components/users-datas/employee-datas').then(m => m.EmployeeDatas) },
      { path: 'approvals',          loadComponent: () => import('./components/approvals/approvals').then(m => m.Approvals) },
      { path: 'notifications',      loadComponent: () => import('./components/notifications/notifications').then(m => m.Notifications) },
    ]
  },

  { path: '**', loadComponent: () => import('./login/login').then(m => m.Login) }
];
