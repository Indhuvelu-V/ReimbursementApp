import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { APIService } from '../services/api.service';
import { ToastService } from '../services/toast.service';
import { TokenService } from '../services/token.service';
import { LoaderService } from '../services/loader.service';
import { ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login {
  private fb           = inject(FormBuilder);
  private apiService   = inject(APIService);
  private router       = inject(Router);
  private toast        = inject(ToastService);
  private tokenService = inject(TokenService);
  loader               = inject(LoaderService);
  themeService         = inject(ThemeService);

  showPassword = false;

  loginForm = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  get username() { return this.loginForm.controls.username; }
  get password() { return this.loginForm.controls.password; }

  login() {
    this.loginForm.markAllAsTouched();
    if (this.loginForm.invalid) {
      this.toast.showWarning('Please fill in all fields correctly.');
      return;
    }

    this.loader.show();
    const payload = {
      userName: this.username.value,
      password: this.password.value,
    };

    this.apiService.apiLogin(payload).subscribe({
      next: (res: any) => {
        sessionStorage.setItem('token', res.token);
        // Store user info for profile display
        if (res.reportingManagerName) {
          sessionStorage.setItem('reportingManagerName', res.reportingManagerName);
          sessionStorage.setItem('reportingManagerId', res.reportingManagerId ?? '');
        } else {
          sessionStorage.removeItem('reportingManagerName');
          sessionStorage.removeItem('reportingManagerId');
        }
        const role = this.tokenService.getRoleFromToken();
        const userId = this.tokenService.getUserIdFromToken();
        this.loader.hide();

        switch (role?.toLowerCase()) {
          case 'employee': this.router.navigate(['/employee']); break;
          case 'teamlead': this.router.navigate(['/teamlead']); break;
          case 'manager':  this.router.navigate(['/manager']);  break;
          case 'admin':    this.router.navigate(['/admin']);    break;
          case 'finance':  this.router.navigate(['/finance']);  break;
          default:
            this.toast.showWarning('Invalid role: ' + role);
            this.router.navigate(['/login']);
        }

        if (res.reportingManagerName) {
          this.toast.show(`Welcome! Your reporting manager is ${res.reportingManagerName} ✅`);
        } else {
          this.toast.show(`Welcome! ${role ?? 'user'} ✅`);
        }
        this.password.setValue('');
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Login failed. Please check your credentials.');
        this.loader.hide();
      }
    });
  }

  togglePassword() { this.showPassword = !this.showPassword; }
  toggleTheme()    { this.themeService.toggle(); }
  reset()          { this.loginForm.reset(); }
}
