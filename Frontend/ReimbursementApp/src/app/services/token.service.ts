import { Injectable } from "@angular/core";
import { jwtDecode } from "jwt-decode";

@Injectable({ providedIn: 'root' })
export class TokenService {

  getRoleFromToken(): string | null {
    const token = sessionStorage.getItem('token');
    if (!token) return null;
    const decoded: any = jwtDecode(token);
    // .NET ClaimTypes.Role maps to this claim key
    return decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || decoded['role']
      || null;
  }

  getUserIdFromToken(): string | null {
    const token = sessionStorage.getItem('token');
    if (!token) return null;
    const decoded: any = jwtDecode(token);
    return decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
      || decoded['nameid']
      || decoded['sub']
      || null;
  }

  getUsernameFromToken(): string | null {
    const token = sessionStorage.getItem('token');
    if (!token) return null;
    const decoded: any = jwtDecode(token);
    return decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
      || decoded['unique_name']
      || decoded['username']
      || null;
  }

  isLoggedIn(): boolean {
    return !!sessionStorage.getItem('token');
  }
}
