using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IApprovalService
    {
        Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request);
        //Task<CreateApprovalResponseDto?> FinanceApproval(string expenseId, string financeId);
      
        Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams);
    }
}