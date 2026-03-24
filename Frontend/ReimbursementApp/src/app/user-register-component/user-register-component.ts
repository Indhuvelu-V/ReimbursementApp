import { Component, inject } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { APIService } from '../services/api.service';
import { UserRegisterModel } from '../models/userregister.model';
import { CreateUserResponseDto, DepartmentType, UserRole } from '../models/user.model';
import { LoaderService } from '../services/loader.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-user-register-component',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './user-register-component.html',
  styleUrls: ['./user-register-component.css']
})
export class UserRegisterComponent {
  registrationModel: UserRegisterModel = new UserRegisterModel();
  roles       = Object.values(UserRole);
  departments = Object.values(DepartmentType);

  router = inject(Router);
  loader = inject(LoaderService);
  toast  = inject(ToastService);

  constructor(private apiService: APIService) {}

  register(form: NgForm) {
    if (form.invalid) {
      this.toast.showWarning('Please fill in all required fields correctly.');
      return;
    }

    this.loader.show();
    this.apiService.apiCreateUser(this.registrationModel).subscribe({
      next: (res: CreateUserResponseDto) => {
        this.loader.hide();
        this.toast.show(`User "${res.userName}" registered successfully! Please log in. ✅`);
        this.router.navigate(['/login']).then(() => {
          form.resetForm({ role: UserRole.Employee, department: DepartmentType.IT });
          this.registrationModel = new UserRegisterModel();
        });
      },
      error: (err) => {
        this.loader.hide();
        this.toast.showError(
          err?.error?.message || 'Registration failed. User ID may already exist.'
        );
      }
    });
  }

  reset(form: NgForm) {
    form.resetForm({ role: UserRole.Employee, department: DepartmentType.IT });
    this.registrationModel = new UserRegisterModel();
  }
}
