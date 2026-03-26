import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { TokenService } from '../services/token.service';

// Maps each protected route to the role that owns it
const ROUTE_ROLE_MAP: Record<string, string> = {
  employee: 'employee',
  manager:  'manager',
  finance:  'finance',
  admin:    'admin',
};

// Maps each role to its own home route
const ROLE_HOME_MAP: Record<string, string> = {
  employee: '/employee',
  manager:  '/manager',
  finance:  '/finance',
  admin:    '/admin',
};

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const tokenService = inject(TokenService);
  const router       = inject(Router);

  // 1. Not logged in → login page
  if (!tokenService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  // 2. Get role from JWT token (lowercase)
  const role = tokenService.getRoleFromToken()?.toLowerCase() ?? '';

  // 3. Which top-level route is being accessed? e.g. 'employee', 'admin'
  const targetRoute = route.url[0]?.path?.toLowerCase() ?? '';

  // 4. Enforce: only the correct role can enter the matching route
  const allowedRole = ROUTE_ROLE_MAP[targetRoute];
  if (allowedRole && role !== allowedRole) {
    // Employee trying /admin, /manager etc. → redirect to their own dashboard
    const home = ROLE_HOME_MAP[role] ?? '/login';
    router.navigate([home]);
    return false;
  }

  return true;
};
