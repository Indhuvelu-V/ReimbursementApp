export interface CreateAuditLogsResponseDto { logId: string; userId: string; userName: string; role: string; action: string; expenseId?: string; amount?: number; oldAmount?: number; documentUrls?: string[]; oldDocumentUrls?: string[]; date: string; description: string; }
export interface PagedResponse<T> { data: T[]; totalRecords: number; pageNumber: number; pageSize: number; }
