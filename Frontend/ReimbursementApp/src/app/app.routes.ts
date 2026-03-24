import { Routes } from '@angular/router';
import { Login } from './login/login';
import { UserRegisterComponent } from './user-register-component/user-register-component';
import { EmployeeDashboard } from './employee-dashboard/employee-dashboard';
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
import { NotFound } from './components/not-found/not-found';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  // Default redirect
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // Public routes
  { path: 'login',    component: Login },
  { path: 'register', component: UserRegisterComponent },

  // Employee routes
  {
    path: 'employee',
    component: EmployeeDashboard,
    canActivate: [authGuard],
    children: [
      { path: '',                  redirectTo: 'policy', pathMatch: 'full' },
      { path: 'expense',           component: Expenses },
      { path: 'policy',            component: Policies },
      { path: 'notifications',     component: Notifications },
      { path: 'expense-categories',component: ExpenseCategories },
      { path: 'payment',           component: Payments },
      { path: 'employee-data',     component: EmployeeDatas },
    ]
  },

  // Manager routes
  {
    path: 'manager',
    component: ManagerDashboard,
    canActivate: [authGuard],
    children: [
      { path: '',                  redirectTo: 'policy', pathMatch: 'full' },
      { path: 'expense',           component: Expenses },
      { path: 'policy',            component: Policies },
      { path: 'approvals',         component: Approvals },
      { path: 'notifications',     component: Notifications },
      { path: 'expense-categories',component: ExpenseCategories },
      { path: 'payment',           component: Payments },
      // { path: 'logs',              component: Logs },
      { path: 'employee-data',     component: EmployeeDatas },
    ]
  },

  // Finance routes
  {
    path: 'finance',
    component: FinanceDashboard,
    canActivate: [authGuard],
    children: [
      { path: '',                  redirectTo: 'policy', pathMatch: 'full' },
      { path: 'expense',           component: Expenses },
      { path: 'expense-categories',component: ExpenseCategories },
      { path: 'policy',            component: Policies },
      // { path: 'log',               component: Logs },
      { path: 'payment',           component: Payments },
      { path: 'employee-data',     component: EmployeeDatas },
      { path: 'notifications',     component: Notifications },
    ]
  },

  // Admin routes
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [authGuard],
    children: [
      { path: '',                  redirectTo: 'policy', pathMatch: 'full' },
      { path: 'expense',           component: Expenses },
      { path: 'expense-categories',component: ExpenseCategories },
      { path: 'policy',            component: Policies },
      { path: 'log',               component: Logs },
      { path: 'payment',           component: Payments },
      { path: 'employee-data',     component: EmployeeDatas },
      { path: 'approvals',         component: Approvals },
      { path: 'notifications',     component: Notifications },
    ]
  },

  // Req 5: Wrong route → Access Denied / Not Found (do NOT crash)
  { path: '**', component: Login }
];
