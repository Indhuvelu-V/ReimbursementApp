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

  private baseUrl = 'http://localhost:5138/api';

  constructor(private http: HttpClient) {}

  // ============================= AUTH =============================
  apiLogin(loginModel: LoginModel): Observable<any> {
    return this.http.post(`${this.baseUrl}/Auth/login`, loginModel);
  }

  apiCreateUser(user: UserRegisterModel): Observable<CreateUserResponseDto> {
    return this.http.post<CreateUserResponseDto>(`${this.baseUrl}/Users/register`, user);
  }

  // ============================= POLICIES =============================
  getPolicies(): Observable<Policy[]> {
    return this.http.get<Policy[]>(`${this.baseUrl}/Policy/getall`);
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

  submitExpense(expenseId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/Expense/Submit/${expenseId}`, {});
  }

  deleteExpense(expenseId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/Expense/${expenseId}`);
  }

  /** Employee: own expenses only */
  getMyExpenses(): Observable<any> {
    // ✅ FIXED — matches your backend [HttpGet("userexpenses")]
    return this.http.get(`${this.baseUrl}/Expense/userexpenses`);
  }

  /** Manager/Finance/Admin: all expenses with pagination */
  getAllExpenses(pageNumber: number = 1, pageSize: number = 10): Observable<{ data: CreateExpenseResponseDto[], totalRecords: number }> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.post<{ data: CreateExpenseResponseDto[], totalRecords: number }>(
      `${this.baseUrl}/Expense/all`, {}, { params }
    );
  }

  // ============================= NOTIFICATIONS =============================

    // ✅ Unified endpoint — works for all roles.
  getMyNotifications(): Observable<CreateNotificationResponseDto[]> {
    return this.http.get<CreateNotificationResponseDto[]>(`${this.baseUrl}/Notification/GetMyNotifications`);
  }

  // ✅ Reply to a notification (Employees)
  replyNotification(data: { notificationId: string; reply: string }): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/Users/reply`, data);
  }

  // ✅ Mark notification as read
  markAsRead(notificationId: string): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/Users/read/${notificationId}`, {});
  }

  // ✅ Manager: create notification for employee
  createNotification(data: { userId: string; message: string }): Observable<CreateNotificationResponseDto> {
    return this.http.post<CreateNotificationResponseDto>(`${this.baseUrl}/Notification/AllUsersCreate`, data);
  }

  // ============================= EXPENSE CATEGORIES =============================
  apiGetAllCategories(): Observable<CreateExpenseCategoryResponseDto[]> {
    return this.http.get<CreateExpenseCategoryResponseDto[]>(`${this.baseUrl}/ExpenseCategory`);
  }

  apiGetCategoryByType(categoryType: string): Observable<CreateExpenseCategoryResponseDto> {
    return this.http.get<CreateExpenseCategoryResponseDto>(`${this.baseUrl}/ExpenseCategory/${categoryType}`);
  }

  /** Admin only: update category max limit (PUT /api/ExpenseCategory) */
  apiUpdateCategory(request: CreateExpenseCategoryRequestDto): Observable<CreateExpenseCategoryResponseDto> {
    return this.http.put<CreateExpenseCategoryResponseDto>(`${this.baseUrl}/ExpenseCategory`, request);
  }

  // ============================= PAYMENTS =============================
  /** Finance/Admin: complete payment for an expense */
  completePayment(expenseId: string, request: { referenceNo: string; paymentMode: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/Payment/CompletePayment/${expenseId}`, request);
  }

  /** Role-based: employee only sees own */
  getPaymentByExpenseId(expenseId: string): Observable<CreatePaymentResponseDto> {
    return this.http.get<CreatePaymentResponseDto>(`${this.baseUrl}/Payment/${expenseId}`);
  }

  /** Finance/Admin: all payments paged (POST /api/Payment) */
  getAllPayments(page: number = 1, pageSize: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('PageNumber', page)
      .set('PageSize', pageSize);
    return this.http.post(`${this.baseUrl}/Payment`, {}, { params });
  }

  // ============================= USERS =============================
  getAllUsers(page: number = 1, size: number = 10): Observable<any> {
    const params = new HttpParams()
      .set('pageNumber', page)
      .set('pageSize', size);
    return this.http.post(`${this.baseUrl}/Users/allusers`, {}, { params });
  }

  getUserById(userId: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/Users/${userId}`);
  }

  // ============================= APPROVALS =============================
  managerApproval(request: CreateApprovalRequestDto): Observable<CreateApprovalResponseDto> {
    return this.http.post<CreateApprovalResponseDto>(`${this.baseUrl}/Approval/manager`, request);
  }

  // NOTE: FinanceApproval removed from backend (req 11) - removed from service too

  getAllApprovals(pagination: PaginationParams): Observable<PagedResponse<CreateApprovalResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pagination.pageNumber)
      .set('pageSize', pagination.pageSize);
    return this.http.get<PagedResponse<CreateApprovalResponseDto>>(`${this.baseUrl}/Approval/all`, { params });
  }

  // ============================= AUDIT LOGS =============================
  
    // Get paged logs with optional date filter
  getPagedLogs(
    pageNumber: number,
    pageSize: number,
    fromDate?: string,
    toDate?: string
  ): Observable<PagedResponse<CreateAuditLogsResponseDto>> {
    const payload: any = { pageNumber, pageSize };
    if (fromDate) payload.fromDate = fromDate;
    if (toDate)   payload.toDate = toDate;

    return this.http.post<PagedResponse<CreateAuditLogsResponseDto>>(
      `${this.baseUrl}/AuditLogs/paged`,
      payload
    );
  }

  // UNCHANGED
  createAuditLog(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/AuditLogs`, data);
  }

  // UNCHANGED
  deleteLog(logId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/AuditLogs/${logId}`);
  }
}
