export interface CreateExpenseResponseDto {
  expenseId: string; categoryId: string; categoryName: string;
  amount: number; expenseDate: string; status: string;
  documentUrls?: string[]; canEdit: boolean;
  message?: string; notificationMessage?: string; amountInRupees?: string;
}
export interface CreateApprovalRequestDto {
  expenseId: string; managerId: string; status: string; comments?: string; level?: string;
}
export interface CreateApprovalResponseDto {
  approvalId: string; expenseId: string; status: string;
  approvedAt?: string; approverName: string; comments?: string; level?: string;
  notificationMessage?: string;
  documentUrls?: string[];
  expenseAmount?: number;
  amountInRupees?: string;
}
export interface PaginationParams { pageNumber: number; pageSize: number; }
export interface PagedResponse<T> { data: T[]; totalRecords: number; pageNumber: number; pageSize: number; totalPages: number; }
export interface CreatePaymentResponseDto {
  paymentId: string; userId: string; userName: string; amountPaid: number;
  paymentStatus: string; paymentMode: string; referenceNo: string;
  paymentDate: string; amountInRupees?: string; expenseId: string;
  documentUrls?: string[];
}
