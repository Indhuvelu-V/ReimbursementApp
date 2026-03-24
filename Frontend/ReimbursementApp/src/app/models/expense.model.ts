export interface CreateExpenseResponseDto {
  expenseId: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  expenseDate: string;
  status: string;
  documentUrls?: string[];
  canEdit: boolean;
  message?: string;
  notificationMessage?: string;
  amountInRupees?: string;
}
