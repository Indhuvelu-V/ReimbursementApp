import { Component } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="nf-wrapper">
      <div class="nf-card">
        <div class="nf-icon"><i class="bi bi-shield-exclamation"></i></div>
        <h2>Access Denied or Page Not Found</h2>
        <p>The page you are looking for does not exist, or you don't have permission to access it.</p>
        <div class="nf-actions">
          <button class="btn-go-back" (click)="goBack()"><i class="bi bi-arrow-left me-2"></i>Go Back</button>
          <button class="btn-go-home" (click)="goHome()"><i class="bi bi-house me-2"></i>Home</button>
        </div>
      </div>
    </div>
  `,
  styles: ['.nf-wrapper{min-height:100vh;display:flex;align-items:center;justify-content:center;background:var(--bg)}.nf-card{background:var(--surface);border:1px solid var(--border);border-radius:var(--r-xl);padding:48px 40px;max-width:480px;width:90%;text-align:center;box-shadow:var(--shadow-xl)}.nf-icon{font-size:4rem;color:var(--status-draft);margin-bottom:18px}h2{color:var(--text);font-size:1.4rem;margin-bottom:12px;font-weight:800}p{color:var(--text-muted);font-size:14px;line-height:1.6;margin-bottom:28px}.nf-actions{display:flex;gap:14px;justify-content:center}.btn-go-back{background:var(--surface2);color:var(--text);border:1px solid var(--border);border-radius:var(--r-sm);padding:10px 22px;font-size:14px;cursor:pointer}.btn-go-home{background:linear-gradient(135deg,var(--accent),var(--violet));color:#fff;border:none;border-radius:var(--r-sm);padding:10px 22px;font-size:14px;cursor:pointer}']
})
export class NotFound {
  constructor(private router: Router, private location: Location) {}
  goBack() { this.location.back(); }
  goHome() { this.router.navigate(['/login']); }
}
