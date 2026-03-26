# Reimbursement Tracker — Angular 19

## Tech Stack
- Angular 19 (standalone components)
- Bootstrap 5.3 + Bootstrap Icons 1.11 (CDN)
- Inter + JetBrains Mono fonts (Google Fonts CDN)
- jwt-decode for token parsing

## Setup

### 1. Install dependencies
```bash
npm install
```

### 2. Start dev server
```bash
ng serve
```
Open `http://localhost:4200`

### 3. Build for production
```bash
ng build
```

---

## Theme System

The app supports **Dark Mode** (default) and **Light Mode**.

- Toggle button is in the **navbar** of every dashboard (☀️ / 🌙)
- Also available floating top-right on Login and Register pages
- Preference is **persisted to localStorage**
- OS preference is auto-detected on first load

### Colours
| Token | Dark | Light |
|---|---|---|
| `--accent` | Emerald `#10b981` | `#059669` |
| `--violet` | `#8b5cf6` | `#7c3aed` |
| `--bg` | `#09090b` | `#f8fafc` |
| `--surface` | `#18181b` | `#ffffff` |

---

## Role-based Dashboards

| Role | Route | Accent Colour |
|---|---|---|
| Employee | `/employee` | Emerald |
| Manager | `/manager` | Sky Blue |
| Finance | `/finance` | Amber |
| Admin | `/admin` | Violet |

---

## Navbar
- **Sticky** — stays at top on scroll (`position: sticky`)
- **Frosted glass** — `backdrop-filter: blur(14px)`
- **Mobile hamburger** — collapses on screens < 992px
- **Per-role accent** — each dashboard has its own brand colour

---

## Project Structure
```
src/
├── index.html
├── main.ts
├── styles.css                  ← All CSS tokens (dark + light)
└── app/
    ├── shared-navbar.css       ← Sticky navbar shared by all dashboards
    ├── app.ts / app.html / app.config.ts / app.routes.ts
    ├── admin-dashboard/
    ├── manager-dashboard/
    ├── employee-dashboard/
    ├── finance-dashboard/
    ├── login/
    ├── user-register-component/
    ├── components/
    │   ├── approvals/
    │   ├── expense-categories/
    │   ├── expense-status-tracker/
    │   ├── expenses/
    │   ├── logs/
    │   ├── notification-bell/
    │   ├── notifications/
    │   ├── not-found/
    │   ├── payments/
    │   ├── policies/
    │   └── users-datas/
    ├── models/         (11 model files)
    ├── services/       (api, loader, toast, token, theme)
    ├── guards/         (auth.guard)
    └── interceptors/   (auth + loader)
```

---

## Backend API
Default base URL: `http://localhost:5138/api`

To change it, update `src/app/services/api.service.ts`:
```ts
private baseUrl = 'http://localhost:5138/api';
```
