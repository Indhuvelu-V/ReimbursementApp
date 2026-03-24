import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Location } from '@angular/common';

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
          <button class="btn-go-back" (click)="goBack()">
            <i class="bi bi-arrow-left me-2"></i>Go Back
          </button>
          <button class="btn-go-home" (click)="goHome()">
            <i class="bi bi-house me-2"></i>Home
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .nf-wrapper {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      background: linear-gradient(135deg, #0f172a, #1e293b);
    }
    .nf-card {
      background: #1e293b; border: 1px solid #334155; border-radius: 20px;
      padding: 48px 40px; max-width: 480px; width: 90%; text-align: center;
      box-shadow: 0 20px 50px rgba(0,0,0,0.7);
    }
    .nf-icon { font-size: 4rem; color: #f59e0b; margin-bottom: 18px; }
    h2 { color: #f1f5f9; font-size: 1.4rem; margin-bottom: 12px; }
    p { color: #94a3b8; font-size: 14px; line-height: 1.6; margin-bottom: 28px; }
    .nf-actions { display: flex; gap: 14px; justify-content: center; flex-wrap: wrap; }
    .btn-go-back {
      background: #334155; color: #e2e8f0; border: none; border-radius: 8px;
      padding: 10px 22px; font-size: 14px; cursor: pointer; transition: background 0.2s;
    }
    .btn-go-back:hover { background: #475569; }
    .btn-go-home {
      background: linear-gradient(45deg, #3b82f6, #2563eb); color: #fff;
      border: none; border-radius: 8px; padding: 10px 22px; font-size: 14px;
      cursor: pointer; transition: all 0.2s;
    }
    .btn-go-home:hover { background: #1d4ed8; transform: translateY(-1px); }
  `]
})
export class NotFound {
  constructor(private router: Router, private location: Location) {}
  goBack() { this.location.back(); }
  goHome() { this.router.navigate(['/login']); }
}
