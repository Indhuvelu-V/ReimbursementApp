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
import { authGuard } from './guards/auth.guard';
import { ApprovalHistory } from './components/approval-history/approval-history';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login',    component: Login },
  { path: 'register', component: UserRegisterComponent },
  {
    path: 'employee', component: EmployeeDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'policy',             component: Policies },
      { path: 'notifications',      component: Notifications },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'payment',            component: Payments },
      { path: 'employee-data',      component: EmployeeDatas },
    ]
  },
  {
    path: 'manager', component: ManagerDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'policy',             component: Policies },
      { path: 'approvals',          component: Approvals },
      { path: 'approval-history',   component: ApprovalHistory },
      { path: 'notifications',      component: Notifications },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'payment',            component: Payments },
      { path: 'employee-data',      component: EmployeeDatas },
    ]
  },
  {
    path: 'finance', component: FinanceDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'payment', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'policy',             component: Policies },
      { path: 'log',                component: Logs },
      { path: 'payment',            component: Payments },
      { path: 'employee-data',      component: EmployeeDatas },
      { path: 'notifications',      component: Notifications },
    ]
  },
  {
    path: 'admin', component: AdminDashboard, canActivate: [authGuard],
    children: [
      { path: '',                   redirectTo: 'expense', pathMatch: 'full' },
      { path: 'expense',            component: Expenses },
      { path: 'expense-categories', component: ExpenseCategories },
      { path: 'policy',             component: Policies },
      { path: 'log',                component: Logs },
      { path: 'payment',            component: Payments },
      { path: 'employee-data',      component: EmployeeDatas },
      { path: 'approvals',          component: Approvals },
      { path: 'notifications',      component: Notifications },
    ]
  },
  { path: '**', component: Login }
];
