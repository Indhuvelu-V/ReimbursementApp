// models/approval.model.ts
export interface CreateApprovalRequestDto {
  expenseId: string;
  managerId: string;
  status: string;
  comments?: string;
  level?: string;
}

export interface CreateApprovalResponseDto {
  approvalId: string;
  expenseId: string;
  status: string;
  approvedAt?: string;
  approverName: string;
  comments?: string;
  level?: string;
  notificationMessage?: string;
}

export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
}

export interface PagedResponse<T> {
  data: T[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
