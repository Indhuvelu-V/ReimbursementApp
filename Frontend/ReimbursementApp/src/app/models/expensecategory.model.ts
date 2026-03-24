// models/expensecategory.model.ts
export interface CreateExpenseCategoryRequestDto {
  categoryName: string;
  maxLimit: number;
}
export interface CreateExpenseCategoryResponseDto {
  categoryId: string;
  categoryName: string;
  maxLimit: number;
}
