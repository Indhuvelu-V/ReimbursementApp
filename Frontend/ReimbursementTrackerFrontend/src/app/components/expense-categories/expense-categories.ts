import { Component, OnInit } from '@angular/core';
import { CreateExpenseCategoryRequestDto, CreateExpenseCategoryResponseDto } from '../../models/expensecategory.model';
import { APIService } from '../../services/api.service';
import { ToastService } from '../../services/toast.service';
import { TokenService } from '../../services/token.service';
import { LoaderService } from '../../services/loader.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-expense-categories',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './expense-categories.html',
  styleUrls: ['./expense-categories.css']
})
export class ExpenseCategories implements OnInit {

  categories:       CreateExpenseCategoryResponseDto[] = [];
  selectedCategory: CreateExpenseCategoryResponseDto | null = null;
  newMaxLimit:      number = 0;
  role:             string | null = null;

  constructor(
    private categoryService: APIService,
    private toast:           ToastService,
    private tokenService:    TokenService,
    private loader:          LoaderService
  ) {}

  ngOnInit(): void {
    this.role = this.tokenService.getRoleFromToken();
    this.loadCategories();
  }

  loadCategories() {
    this.loader.show();
    this.categoryService.apiGetAllCategories().subscribe({
      next:  (data) => { this.categories = data; this.loader.hide(); },
      error: ()     => { this.toast.showError('Failed to load categories. Please refresh.'); this.loader.hide(); }
    });
  }

  selectCategory(category: CreateExpenseCategoryResponseDto) {
    if (!this.isAdmin()) return;
    this.selectedCategory = { ...category };
    this.newMaxLimit      = category.maxLimit;
  }

  updateCategory() {
    if (!this.selectedCategory || !this.isAdmin()) return;
    if (this.newMaxLimit <= 0) { this.toast.showWarning('Max limit must be greater than zero.'); return; }

    const request: CreateExpenseCategoryRequestDto = {
      categoryName: this.selectedCategory.categoryName,
      maxLimit:     this.newMaxLimit
    };

    this.loader.show();
    this.categoryService.apiUpdateCategory(request).subscribe({
      next: (res) => {
        this.toast.show(`Category "${res.categoryName}" updated to ₹${res.maxLimit.toLocaleString()} ✅`);
        this.selectedCategory = null;
        this.loadCategories();
        this.loader.hide();
      },
      error: (err) => {
        this.toast.showError(err?.error?.message || 'Failed to update category. Please check the values.');
        this.loader.hide();
      }
    });
  }

  isAdmin(): boolean { return this.role === 'Admin'; }
  cancelEdit() { this.selectedCategory = null; }
}
