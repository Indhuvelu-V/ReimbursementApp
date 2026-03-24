

using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<string, AuditLog> _auditLogRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IRepository<string, AuditLog> auditLogRepo, IHttpContextAccessor httpContextAccessor)
        {
            _auditLogRepo = auditLogRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        // Extract user info from JWT token
        private (string userId, string userName, UserRole role) GetUserFromToken()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                throw new UnauthorizedAccessException("User not authenticated.");

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = user.FindFirstValue(ClaimTypes.Name);
            var roleStr = user.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(roleStr))
                throw new UnauthorizedAccessException("Invalid token.");

            if (!Enum.TryParse<UserRole>(roleStr, out var role))
                throw new UnauthorizedAccessException("Invalid role in token.");

            return (userId, userName, role);
        }

        // Create a new audit log
        public async Task<CreateAuditLogsResponseDto> CreateLog(CreateAuditLogsRequestDto request)
        {
            string userId;
            string userName;
            UserRole role;

            // ✅ If user info is passed (e.g., REGISTER) → use it
            if (!string.IsNullOrEmpty(request.UserId) &&
                !string.IsNullOrEmpty(request.UserName) &&
                request.Role.HasValue)
            {
                userId = request.UserId;
                userName = request.UserName;
                role = request.Role.Value;
            }
            else
            {
                // ✅ Otherwise → use token
                var tokenData = GetUserFromToken();
                userId = tokenData.userId;
                userName = tokenData.userName;
                role = tokenData.role;
            }

            var log = new AuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                UserId = userId,
                UserName = userName,
                Role = role,
                Action = request.Action,
                ExpenseId = request.ExpenseId,
                Amount = request.Amount,
                OldAmount = request.OldAmount,
                DocumentUrls = request.DocumentUrls ?? new List<string>(),
                OldDocumentUrls = request.OldDocumentUrls ?? new List<string>(),
                Date = DateTime.UtcNow
            };

            await _auditLogRepo.AddAsync(log);

            return new CreateAuditLogsResponseDto
            {
                LogId = log.LogId,
                UserId = log.UserId,
                UserName = log.UserName,
                Role = log.Role,
                Action = log.Action,
                ExpenseId = log.ExpenseId,
                Amount = log.Amount,
                OldAmount = log.OldAmount,
                DocumentUrls = log.DocumentUrls,
                OldDocumentUrls = log.OldDocumentUrls,
                Date = log.Date,
                Description = $"Action performed: {log.Action}"
            };
        }

        // Get paged logs
        public async Task<PagedResponse<CreateAuditLogsResponseDto>> GetAllLogs(PaginationParams paginationParams)
        {
            var allLogs = await _auditLogRepo.GetAllAsync();
            var logList = allLogs?.ToList() ?? new List<AuditLog>();

            DateTime? from = null;
            DateTime? to = null;

            if (!string.IsNullOrEmpty(paginationParams.FromDate))
                from = DateTime.Parse(paginationParams.FromDate).Date;

            if (!string.IsNullOrEmpty(paginationParams.ToDate))
                to = DateTime.Parse(paginationParams.ToDate).Date.AddDays(1).AddTicks(-1);

            var filteredLogs = logList
                .Where(l =>
                    !string.IsNullOrEmpty(l.Action) &&
                    !l.Action.ToLower().Contains("get") &&
                    !l.Action.ToLower().Contains("fetch") &&
                    !l.Action.ToLower().Contains("view") &&
                    (from == null || l.Date >= from) &&
                    (to == null || l.Date <= to)
                )
                .ToList();

            var totalRecords = filteredLogs.Count;
            var pageNumber = paginationParams.PageNumber < 1 ? 1 : paginationParams.PageNumber;
            var pageSize = paginationParams.PageSize < 1 ? 10 : paginationParams.PageSize;

            var pagedLogs = filteredLogs
                .OrderByDescending(l => l.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new CreateAuditLogsResponseDto
                {
                    LogId = log.LogId,
                    UserId = log.UserId ?? string.Empty,
                    UserName = log.UserName,
                    Role = log.Role,
                    Action = log.Action,
                    ExpenseId = log.ExpenseId,
                    Amount = log.Amount,
                    OldAmount = log.OldAmount,
                    DocumentUrls = log.DocumentUrls,
                    OldDocumentUrls = log.OldDocumentUrls,
                    Date = DateTime.SpecifyKind(log.Date, DateTimeKind.Utc),
                    Description = $"Action performed: {log.Action}"

                })
                .ToList();

            return new PagedResponse<CreateAuditLogsResponseDto>(
                pagedLogs,
                totalRecords,
                pageNumber,
                pageSize
            );
        }

        // Delete a log (only Admin role)
        public async Task<bool> DeleteLog(string logId)
        {
            var tokenData = GetUserFromToken();
            if (tokenData.role != UserRole.Admin)
                throw new UnauthorizedAccessException("Only admins can delete audit logs.");

            var log = await _auditLogRepo.GetByIdAsync(logId);
            if (log == null) return false;

            await _auditLogRepo.DeleteAsync(logId);
            return true;
        }
    }
}