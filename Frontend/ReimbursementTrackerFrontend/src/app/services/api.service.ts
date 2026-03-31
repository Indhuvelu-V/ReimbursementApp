import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginModel } from '../models/login.model';
import { UserRegisterModel } from '../models/userregister.model';
import { CreateUserResponseDto } from '../models/user.model';
import { Policy } from '../models/policy.model';
import { CreateExpenseResponseDto } from '../models/expense.model';
import { CreateNotificationResponseDto } from '../models/notification.model';
import { CreateExpenseCategoryRequestDto, CreateExpenseCategoryResponseDto } from '../models/expensecategory.model';
import { CreatePaymentResponseDto } from '../models/payment.model';
import { User } from '../models/employeedata.model';
import { CreateApprovalRequestDto, CreateApprovalResponseDto, PagedResponse, PaginationParams } from '../models/approval.model';
import { CreateAuditLogsResponseDto } from '../models/log.model';

@Injectable({ providedIn: 'root' })
export class APIService {

  private baseUrl   = 'http://localhost:5138/api';
  readonly staticBase = 'http://localhost:5138';

  constructor(private http: HttpClient) {}

  // ============================= AUTH =============================

  apiLogin(loginModel: LoginModel): Observable<any> {
    return this.http.post(`${this.baseUrl}/Auth/login`, loginModel);
  }

  // ============================= USERS =============================

  apiCreateUser(user: UserRegisterModel): Observable<CreateUserResponseDto> {
    return this.http.post<CreateUserResponseDto>(`${this.baseUrl}/Users/register`, user);
  }

  getAllUsers(page: number = 1, size: number = 10, role?: string, name?: string): Observable<any> {
    let params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    if (role) params = params.set('Role', role);
    if (name) params = params.set('Name', name);
    return this.http.get(`${this.baseUrl}/Users/allusers`, { params });
  }

  getUserById(userId: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/Users/${userId}`);
  }

  // ============================= EXPENSES =============================

  createExpense(formData: FormData): Observable<any> {
    return this.http.post(`${this.baseUrl}/Expense/Create`, formData);
  }

  updateExpense(expenseId: string, formData: FormData): Observable<any> {
    return this.http.put(`${this.baseUrl}/Expense/${expenseId}`, formData);
  }

  getExpenseById(expenseId: string): Observable<CreateExpenseResponseDto> {
    return this.http.get<CreateExpenseResponseDto>(`${this.baseUrl}/Expense/${expenseId}`);
  }

  getMyExpenses(): Observable<any> {
    return this.http.get(`${this.baseUrl}/Expense/userexpenses`);
  }

  getAllExpenses(
    pageNumber: number = 1,
    pageSize: number = 10,
    status?: string,
    fromDate?: string,
    toDate?: string,
    minAmount?: number | null,
    maxAmount?: number | null,
    userName?: string
  ): Observable<{ data: any[]; totalRecords: number }> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status)              params = params.set('Status', status);
    if (fromDate)            params = params.set('FromDate', fromDate);
    if (toDate)              params = params.set('ToDate', toDate);
    if (minAmount != null)   params = params.set('MinAmount', minAmount);
    if (maxAmount != null)   params = params.set('MaxAmount', maxAmount);
    if (userName)            params = params.set('UserName', userName);
    return this.http.get<{ data: any[]; totalRecords: number }>(`${this.baseUrl}/Expense/all`, { params });
  }

  submitExpense(expenseId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/Expense/Submit/${expenseId}`, {});
  }

  resubmitExpense(expenseId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/Expense/Resubmit/${expenseId}`, {});
  }

  deleteExpense(expenseId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/Expense/${expenseId}`);
  }

  // ============================= EXPENSE CATEGORIES =============================

  apiGetAllCategories(): Observable<CreateExpenseCategoryResponseDto[]> {
    return this.http.get<CreateExpenseCategoryResponseDto[]>(`${this.baseUrl}/ExpenseCategory`);
  }

  apiGetCategoryByType(categoryType: string): Observable<CreateExpenseCategoryResponseDto> {
    return this.http.get<CreateExpenseCategoryResponseDto>(`${this.baseUrl}/ExpenseCategory/${categoryType}`);
  }

  /** Admin only: update category max limit */
  apiUpdateCategory(request: CreateExpenseCategoryRequestDto): Observable<CreateExpenseCategoryResponseDto> {
    return this.http.put<CreateExpenseCategoryResponseDto>(`${this.baseUrl}/ExpenseCategory`, request);
  }

  // ============================= APPROVALS =============================

  managerApproval(request: CreateApprovalRequestDto): Observable<CreateApprovalResponseDto> {
    return this.http.post<CreateApprovalResponseDto>(`${this.baseUrl}/Approval/manager`, request);
  }

  getAllApprovals(pagination: PaginationParams): Observable<PagedResponse<CreateApprovalResponseDto>> {
    let params = new HttpParams()
      .set('pageNumber', pagination.pageNumber)
      .set('pageSize', pagination.pageSize);
    if (pagination.userName) params = params.set('UserName', pagination.userName);
    return this.http.get<PagedResponse<CreateApprovalResponseDto>>(`${this.baseUrl}/Approval/all`, { params });
  }

  // ============================= PAYMENTS =============================

  completePayment(expenseId: string, request: { referenceNo: string; paymentMode: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/Payment/CompletePayment/${expenseId}`, request);
  }

  getPaymentByExpenseId(expenseId: string): Observable<CreatePaymentResponseDto> {
    return this.http.get<CreatePaymentResponseDto>(`${this.baseUrl}/Payment/${expenseId}`);
  }

  getAllPayments(
    page: number = 1,
    pageSize: number = 10,
    fromDate?: string,
    toDate?: string,
    minAmount?: number | null,
    maxAmount?: number | null,
    status?: string,
    userName?: string
  ): Observable<any> {
    let params = new HttpParams().set('PageNumber', page).set('PageSize', pageSize);
    if (fromDate)          params = params.set('FromDate', fromDate);
    if (toDate)            params = params.set('ToDate', toDate);
    if (minAmount != null) params = params.set('MinAmount', minAmount);
    if (maxAmount != null) params = params.set('MaxAmount', maxAmount);
    if (status)            params = params.set('Status', status);
    if (userName)          params = params.set('UserName', userName);
    return this.http.get(`${this.baseUrl}/Payment`, { params });
  }

  // ============================= NOTIFICATIONS =============================

  getMyNotifications(): Observable<CreateNotificationResponseDto[]> {
    return this.http.get<CreateNotificationResponseDto[]>(`${this.baseUrl}/Notification/GetMyNotifications`);
  }

  getSentNotifications(): Observable<CreateNotificationResponseDto[]> {
    return this.http.get<CreateNotificationResponseDto[]>(`${this.baseUrl}/Notification/GetSentNotifications`);
  }

  markSentAsRead(notificationId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/Notification/Sender/read/${notificationId}`, {});
  }

  replyNotification(data: { notificationId: string; reply: string }): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/Users/reply`, data);
  }

  markAsRead(notificationId: string): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/Users/read/${notificationId}`, {});
  }

  createNotification(data: { userId: string; message: string }): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/AllUsersCreate`, data);
  }

  // ============================= POLICIES =============================

  getPolicies(): Observable<Policy[]> {
    return this.http.get<Policy[]>(`${this.baseUrl}/Policy/getall`);
  }

  // ============================= FILE UPLOAD =============================

  uploadFiles(formData: FormData): Observable<any> {
    return this.http.post(`${this.baseUrl}/FileUpload`, formData);
  }

  // ============================= AUDIT LOGS =============================

  getPagedLogs(
    pageNumber: number,
    pageSize: number,
    fromDate?: string,
    toDate?: string,
    userName?: string,
    action?: string
  ): Observable<PagedResponse<CreateAuditLogsResponseDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate)   params = params.set('toDate', toDate);
    if (userName) params = params.set('userName', userName);
    if (action)   params = params.set('action', action);
    return this.http.get<PagedResponse<CreateAuditLogsResponseDto>>(`${this.baseUrl}/AuditLogs/paged`, { params });
  }

  createAuditLog(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/AuditLogs`, data);
  }

  deleteLog(logId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/AuditLogs/${logId}`);
  }

  // ============================= FILE HELPERS =============================

  /** Resolve a backend relative path to a full URL. e.g. /uploads/abc.png → http://localhost:5138/uploads/abc.png */
  resolveFileUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `${this.staticBase}${path}`;
  }

  isImageUrl(path: string): boolean {
    const ext = (path || '').split('.').pop()?.toLowerCase() ?? '';
    return ['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp'].includes(ext);
  }

  fileIcon(path: string): string {
    const ext = (path || '').split('.').pop()?.toLowerCase() ?? '';
    if (ext === 'pdf')                    return 'bi-file-earmark-pdf';
    if (['xlsx', 'xls'].includes(ext))    return 'bi-file-earmark-excel';
    if (['doc', 'docx'].includes(ext))    return 'bi-file-earmark-word';
    return 'bi-file-earmark';
  }

  fileName(path: string): string {
    return (path || '').split('/').pop() ?? path;
  }
}
