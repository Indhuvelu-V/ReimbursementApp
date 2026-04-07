
<<<<<<< HEAD
import { inject } from "@angular/core";
import { CanActivateFn, Router, UrlTree } from "@angular/router";
import { TokenService } from "../services/token.service";

export const authGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const token = sessionStorage.getItem('token');
=======
// Maps each protected route to the role that owns it
const ROUTE_ROLE_MAP: Record<string, string> = {
  employee: 'employee',
  teamlead: 'teamlead',
  manager:  'manager',
  finance:  'finance',
  admin:    'admin',
};

// Maps each role to its own home route
const ROLE_HOME_MAP: Record<string, string> = {
  employee: '/employee',
  teamlead: '/teamlead',
  manager:  '/manager',
  finance:  '/finance',
  admin:    '/admin',
};

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
>>>>>>> eba5464 (Feature added)
  const tokenService = inject(TokenService);
  const router = inject(Router);

  if (!token) {
    return router.createUrlTree(['/login']); // not logged in
  }

  const role = tokenService.getRoleFromToken();
  console.log('User role from token:', role);

  if (!role) {
    return router.createUrlTree(['/login']);
  }

  const roleDashboardMap: Record<string, string> = {
    Employee: '/employee',
    Manager: '/manager',
    Admin: '/admin',
    Finance: '/finance'
  };

  const allowedDashboard = roleDashboardMap[role];

  if (!allowedDashboard) {
    return router.createUrlTree(['/login']);
  }

  // ✅ Allow all child routes of dashboard by using startsWith
  if (state.url.startsWith(allowedDashboard)) {
    return true;
  }

  // Redirect to the allowed dashboard
  return router.createUrlTree([allowedDashboard]);
};