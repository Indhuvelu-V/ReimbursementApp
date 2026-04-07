using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IApprovalService
    {
        // Level1: TeamLead reviews Submitted expenses from Employees/TeamLeads
        Task<CreateApprovalResponseDto?> TeamLeadApproval(CreateApprovalRequestDto request);

        // Level2: Manager reviews Pending (post-TeamLead) or Submitted (Manager's own) expenses
        Task<CreateApprovalResponseDto?> ManagerApproval(CreateApprovalRequestDto request);
<<<<<<< HEAD
        Task<CreateApprovalResponseDto?> AdminApproval(CreateApprovalRequestDto request);
=======

        // Admin view
>>>>>>> eba5464 (Feature added)
        Task<PagedResponse<CreateApprovalResponseDto>> GetAllApprovals(PaginationParams paginationParams);

        // Fetch expenses awaiting TeamLead review (status = Submitted, submitted by Employee or TeamLead)
        Task<List<CreateExpenseResponseDto>> GetExpensesPendingTeamLeadApproval();

        // Fetch expenses awaiting Manager review (status = Pending or Submitted by Manager)
        Task<List<CreateExpenseResponseDto>> GetExpensesPendingManagerApproval();
    }
}
