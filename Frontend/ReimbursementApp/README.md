# Reimbursement Tracker — Angular Frontend

A fully-featured Angular 19 frontend for the Reimbursement Tracker .NET backend.

## 🚀 Quick Start

```bash
# 1. Install dependencies
npm install

# 2. Start dev server
npm start
# OR
ng serve

# 3. Open browser
# http://localhost:4200
```

## 🔧 Backend URL

The API base URL is configured in:
```
src/app/services/api.service.ts
```
```typescript
private baseUrl = 'http://localhost:5000/api';
```
Change this to match your backend port if different.

## 📁 Project Structure

```
src/
├── app/
│   ├── admin-dashboard/          Admin layout & navbar
│   ├── employee-dashboard/       Employee layout & navbar
│   ├── finance-dashboard/        Finance layout & navbar
│   ├── manager-dashboard/        Manager layout & navbar
│   ├── login/                    Login page
│   ├── user-register-component/  Registration page
│   ├── components/
│   │   ├── approvals/            Approval workflow
│   │   ├── expense-categories/   Category management
│   │   ├── expense-status-tracker/ Status stepper UI
│   │   ├── expenses/             Expense CRUD
│   │   ├── logs/                 Audit logs (Admin)
│   │   ├── not-found/            404 / Access Denied page
│   │   ├── notification-bell/    Bell icon with dropdown
│   │   ├── notifications/        Full notifications page
│   │   ├── payments/             Payment management
│   │   ├── policies/             Policy viewer
│   │   └── users-datas/          User data (Admin)
│   ├── guards/                   Auth guard
│   ├── interceptors/             JWT + Loader interceptors
│   ├── models/                   All TypeScript interfaces
│   └── services/                 API, Token, Toast, Loader
├── index.html
├── main.ts
└── styles.css
```

## 👥 Role-Based Access

| Role     | Default Route  | Access                              |
|----------|---------------|-------------------------------------|
| Employee | /employee     | Own expenses, notifications, payment lookup |
| Manager  | /manager      | All expenses, approvals, notifications |
| Finance  | /finance      | Payments, expenses                  |
| Admin    | /admin        | Full access, logs, categories, users |

## ✅ Features

- 🔔 Notification bell with dropdown (auto-refreshes every 15s)
- 📊 Expense status tracker: Draft → Submitted → Approved → Paid
- 🍞 Typed toasts: success ✅ / error ❌ / warning ⚠️ / info ℹ️
- 🔄 Global spinner that stops immediately on toast
- 📱 Responsive hamburger menu on all dashboards
- ❌ Access Denied page for wrong routes
- 💰 Finance payment form with approved expense dropdown
- 📜 Logs with delete confirmation popup & fixed amount display
- 🔐 JWT role-based routing on login
