

using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<string, Notification> _notificationRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<NotificationService> _logger; // Added ILogger

        public NotificationService(
            IRepository<string, Notification> notificationRepo,
            IAuditLogService auditLogService,
            ILogger<NotificationService> logger) // Inject ILogger
        {
            _notificationRepo = notificationRepo;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // =====================================================
        // CREATE NOTIFICATION
        // SenderRole "Manager" → manual manager message, reply allowed on frontend
        // SenderRole "System" → auto-fired (Approved / Paid), no reply shown
        // =====================================================
        public async Task<CreateNotificationResponseDto> CreateNotification(CreateNotificationRequestDto request)
        {
            _logger.LogInformation("Creating notification for User {UserId} by {SenderRole}", request.UserId, request.SenderRole);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId         = request.UserId,
                SenderId       = request.SenderId ?? string.Empty,
                Message        = request.Message,
                Description    = request.Description ?? string.Empty,
                ReadStatus     = "Unread",
                SenderRole     = request.SenderRole ?? "Manager",
                CreatedAt      = DateTime.UtcNow
            };

            await _notificationRepo.AddAsync(notification);
            _logger.LogInformation("Notification {NotificationId} created successfully for User {UserId}", notification.NotificationId, request.UserId);

            // Audit log — wrapped so a log failure never breaks the notification itself
            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Notification created for user {request.UserId}",
                    Date = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed for notification {NotificationId}", notification.NotificationId);
            }

            return MapToDto(notification);
        }

        // =====================================================
        // GET NOTIFICATIONS BY USER (ordered newest first)
        // =====================================================
        public async Task<IEnumerable<CreateNotificationResponseDto>> GetNotificationsByUser(string userId)
        {
            _logger.LogInformation("Fetching notifications for User {UserId}", userId);

            var all = await _notificationRepo.GetAllAsync();

            var userNotifs = all?
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList() ?? new List<Notification>();

            _logger.LogInformation("Fetched {Count} notifications for User {UserId}", userNotifs.Count, userId);

            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Viewed notifications for user {userId}",
                    Date = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed while fetching notifications for User {UserId}", userId);
            }

            return userNotifs.Select(MapToDto);
        }

        // =====================================================
        // MARK AS READ
        // =====================================================
        public async Task<CreateNotificationResponseDto?> MarkAsRead(string notificationId, string userId)
        {
            _logger.LogInformation("User {UserId} marking notification {NotificationId} as read", userId, notificationId);

            var all = await _notificationRepo.GetAllAsync();
            var notification = all?.FirstOrDefault(n =>
                n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for User {UserId}", notificationId, userId);
                return null;
            }

            if (notification.ReadStatus == "Read")
            {
                _logger.LogInformation("Notification {NotificationId} already marked as read", notificationId);
                return MapToDto(notification);
            }

            notification.ReadStatus = "Read";
            await _notificationRepo.UpdateAsync(notification.NotificationId, notification);

            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"User {userId} marked notification {notificationId} as read",
                    Date = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed while marking notification {NotificationId} as read", notificationId);
            }

            return MapToDto(notification);
        }

        // =====================================================
        // REPLY TO NOTIFICATION
        // Only allowed for Manager-sent notifications
        // =====================================================
        public async Task<CreateNotificationResponseDto?> ReplyNotification(
            ReplyNotificationRequestDto request, string employeeId)
        {
            _logger.LogInformation("User {UserId} replying to notification {NotificationId}", employeeId, request.NotificationId);

            var all = await _notificationRepo.GetAllAsync();
            var notification = all?.FirstOrDefault(n =>
                n.NotificationId == request.NotificationId && n.UserId == employeeId);

            if (notification == null)
            {
                _logger.LogWarning("Reply blocked: Notification {NotificationId} not found for User {UserId}",
                    request.NotificationId, employeeId);
                return null;
            }

            notification.Reply = request.Reply;
            notification.ReadStatus = "Read";

            await _notificationRepo.UpdateAsync(notification.NotificationId, notification);

            try
            {
                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Employee {employeeId} replied to notification {notification.NotificationId}",
                    Date = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed for reply to notification {NotificationId}", notification.NotificationId);
            }

            _logger.LogInformation("User {UserId} successfully replied to notification {NotificationId}", employeeId, notification.NotificationId);

            return MapToDto(notification);
        }

        // =====================================================
        // GET SENT NOTIFICATIONS (messages this user sent)
        // =====================================================
        public async Task<IEnumerable<CreateNotificationResponseDto>> GetSentNotifications(string senderId)
        {
            var all = await _notificationRepo.GetAllAsync();
            return all?
                .Where(n => n.SenderId == senderId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(MapToDto)
                .ToList() ?? new List<CreateNotificationResponseDto>();
        }

        // =====================================================
        // MARK SENT NOTIFICATION AS READ BY SENDER
        // =====================================================
        public async Task MarkSentAsRead(string notificationId, string senderId)
        {
            var all = await _notificationRepo.GetAllAsync();
            var notification = all?.FirstOrDefault(n =>
                n.NotificationId == notificationId && n.SenderId == senderId);
            if (notification == null) return;
            notification.ReadStatus = "Read";
            await _notificationRepo.UpdateAsync(notification.NotificationId, notification);
        }

        // =====================================================
        // PRIVATE MAPPER — single source of truth for DTO mapping
        // =====================================================
        private static CreateNotificationResponseDto MapToDto(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            UserId         = n.UserId,
            SenderId       = n.SenderId,
            Message        = n.Message,
            Reply          = n.Reply,
            Description    = n.Description,
            ReadStatus     = n.ReadStatus,
            SenderRole     = n.SenderRole,
            CreatedAt      = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
        };
    }
}
