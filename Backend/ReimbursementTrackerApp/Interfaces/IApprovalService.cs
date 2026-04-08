using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IApprovalService
    {
        // Stage-based approval actions
        Task<CreateApprovalResponseDto?> TeamLeadApproval(CreateApprovalRequestDto request);
        Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request);
        Task<CreateApprovalResponseDto?> FinanceApproval(CreateApprovalRequestDto request);

        // Legacy admin approval for Manager/Finance-submitted expenses
        Task<CreateApprovalResponseDto?> AdminApproval(CreateApprovalRequestDto request);

        Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams);

        // Get pending approvals for the current approver's stage
        Task<List<CreateApprovalResponseDto>> GetPendingApprovalsForMe(string approverId);

        // Get all approvals made BY this approver (their own history)
        Task<List<CreateApprovalResponseDto>> GetMyApprovalHistory(string approverId);
    }
}
