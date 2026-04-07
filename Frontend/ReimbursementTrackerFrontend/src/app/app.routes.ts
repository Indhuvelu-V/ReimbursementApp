import { Routes } from '@angular/router';
<<<<<<< HEAD
=======
import { Login } from './login/login';
import { UserRegisterComponent } from './user-register-component/user-register-component';
import { EmployeeDashboard } from './employee-dashboard/employee-dashboard';
import { TeamLeadDashboard } from './teamlead-dashboard/teamlead-dashboard';
import { ManagerDashboard } from './manager-dashboard/manager-dashboard';
import { FinanceDashboard } from './finance-dashboard/finance-dashboard';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { Policies } from './components/policies/policies';
import { Expenses } from './components/expenses/expenses';
import { Notifications } from './components/notifications/notifications';
import { ExpenseCategories } from './components/expense-categories/expense-categories';
import { Payments } from './components/payments/payments';
import { EmployeeDatas } from './components/users-datas/employee-datas';
import { Logs } from './components/logs/logs';
import { Approvals } from './components/approvals/approvals';
>>>>>>> eba5464 (Feature added)
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  {
    path: 'login',
    loadComponent: () => import('./login/login').then(m => m.Login)
  },
  {
<<<<<<< HEAD
    path: 'register',
    loadComponent: () => import('./user-register-component/user-register-component').then(m => m.UserRegisterComponent)
=======
    path: 'teamlead', component: TeamLeadDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'approvals',          component: Approvals },
      { path: 'policy',             component: Policies },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'payment',            component: Payments },
      { path: 'notifications',      component: Notifications },
    ]
  },
  {
    path: 'manager', component: ManagerDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'policy',             component: Policies },
      { path: 'approvals',          component: Approvals },
      { path: 'notifications',      component: Notifications },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'payment',            component: Payments },
      { path: 'employee-data',      component: EmployeeDatas },
    ]
>>>>>>> eba5464 (Feature added)
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
