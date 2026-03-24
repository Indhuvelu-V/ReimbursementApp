import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { APIService } from '../services/api.service';
import { ToastService } from '../services/toast.service';
import { TokenService } from '../services/token.service';
import { LoaderService } from '../services/loader.service';

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
      // ✅ Save token
      sessionStorage.setItem('token', res.token);

      // ✅ Extract role
      const role = this.tokenService.getRoleFromToken();
      console.log('ROLE:', role);

      this.loader.hide();

      // ✅ Role-based navigation (same as reference)
      switch (role?.toLowerCase()) {
        case 'employee':
          this.router.navigate(['/employee']);
          break;
        case 'manager':
          this.router.navigate(['/manager']);
          break;
        case 'admin':
          this.router.navigate(['/admin']);
          break;
        case 'finance':
          this.router.navigate(['/finance']);
          break;
        default:
          this.toast.showWarning('Invalid role: ' + role);
          this.router.navigate(['/login']);
          break;
      }

      // ✅ Success toast
      this.toast.show(`Welcome ! Logged in as ${role ?? 'user'} ✅`);

      // Clear password
      this.password.setValue('');
    },

    error: (err) => {
      this.toast.showError(
        err?.error?.message || 'Login failed. Please check your credentials.'
      );
      this.loader.hide();
    }
  });
}


  togglePassword() { this.showPassword = !this.showPassword; }

  reset() { this.loginForm.reset(); }
}
