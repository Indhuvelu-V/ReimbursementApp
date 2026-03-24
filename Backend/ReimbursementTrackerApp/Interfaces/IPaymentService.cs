
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IPaymentService
    {
       
        Task<CreatePaymentResponseDto?> CompletePayment(
            string expenseId,
            string referenceNo,
            string paymentMode
            );
       

        Task<PagedResponse<CreatePaymentResponseDto>> GetAllPayments(PaginationParams paginationParams);


   
        Task<CreatePaymentResponseDto?> GetPaymentByExpenseId(
            string expenseId,
            string userId,
            string role);
    }
}