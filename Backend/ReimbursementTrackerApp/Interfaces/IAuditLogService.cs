
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IAuditLogService
    {
        Task<CreateAuditLogsResponseDto> CreateLog(CreateAuditLogsRequestDto request);
        Task<PagedResponse<CreateAuditLogsResponseDto>> GetAllLogs(PaginationParams paginationParams);
       
        // Delete log (Admin only)
        Task<bool> DeleteLog(string logId);
      
    }
}
