
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface INotificationService
    {
        Task<CreateNotificationResponseDto> CreateNotification(CreateNotificationRequestDto request);

        Task<IEnumerable<CreateNotificationResponseDto>> GetNotificationsByUser(string userId);

        Task<CreateNotificationResponseDto?> ReplyNotification(ReplyNotificationRequestDto request, string employeeId);

        // ✅ NEW — was missing, fixes the broken MarkAsRead controller endpoint
        Task<CreateNotificationResponseDto?> MarkAsRead(string notificationId, string userId);
    }
}
