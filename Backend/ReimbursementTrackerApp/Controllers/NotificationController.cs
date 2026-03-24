

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationController> _logger; // ✅ logger

        public NotificationController(
            INotificationService service,
            ILogger<NotificationController> logger) // ✅ inject
        {
            _service = service;
            _logger = logger;
        }

        // =====================================================
        // 1️⃣ CREATE NOTIFICATION
        // =====================================================
        [HttpPost("AllUsersCreate")]
        [Authorize(Roles = "Manager,Finance,Admin,Employee")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequestDto request)
        {
            _logger.LogInformation("Request to create notification for User {UserId}", request.UserId);

            try
            {
                var result = await _service.CreateNotification(request);

                _logger.LogInformation("Notification created successfully for User {UserId}", request.UserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for User {UserId}", request.UserId);

                return StatusCode(500, new
                {
                    message = "Failed to create notification.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // 2️⃣ GET MY NOTIFICATIONS
        // =====================================================
        [HttpGet("GetMyNotifications")]
        [Authorize(Roles = "Employee,Manager,Admin,Finance")]
        public async Task<IActionResult> GetMyNotifications()
        {
            _logger.LogInformation("Request to fetch user notifications");

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                var result = await _service.GetNotificationsByUser(userId);

                _logger.LogInformation("Fetched {Count} notifications for User {UserId}", result?.Count() ?? 0, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return StatusCode(500, new
                {
                    message = "Failed to fetch notifications.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // 2️⃣ LEGACY ROUTE
        // =====================================================
        [HttpGet("GetUsersNotifications")]
        [Authorize(Roles = "Manager,Finance,Admin,Employee")]
        public async Task<IActionResult> GetMyNotificationsLegacy()
        {
            _logger.LogInformation("Legacy route hit for fetching notifications");
            return await GetMyNotifications();
        }

        // =====================================================
        // 3️⃣ REPLY TO NOTIFICATION
        // =====================================================
        [HttpPost("Users/reply")]
        [Authorize(Roles = "Manager,Finance,Admin,Employee")]
        public async Task<IActionResult> ReplyNotification([FromBody] ReplyNotificationRequestDto request)
        {
            _logger.LogInformation("Request to reply to Notification {NotificationId}", request.NotificationId);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in token while replying");
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                var result = await _service.ReplyNotification(request, userId);

                if (result == null)
                {
                    _logger.LogWarning("Reply failed for Notification {NotificationId}", request.NotificationId);
                    return NotFound(new { message = "Notification not found or replies are not allowed." });
                }

                _logger.LogInformation("Reply successful for Notification {NotificationId}", request.NotificationId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to Notification {NotificationId}", request.NotificationId);
                return StatusCode(500, new
                {
                    message = "Failed to reply to notification.",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // 4️⃣ MARK AS READ
        // =====================================================
        [HttpPost("Users/read/{notificationId}")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            _logger.LogInformation("Request to mark Notification {NotificationId} as read", notificationId);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in token while marking read");
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                var result = await _service.MarkAsRead(notificationId, userId);

                if (result == null)
                {
                    _logger.LogWarning("Notification {NotificationId} not found for marking read", notificationId);
                    return NotFound(new { message = "Notification not found." });
                }

                _logger.LogInformation("Notification {NotificationId} marked as read by User {UserId}", notificationId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking Notification {NotificationId} as read", notificationId);
                return StatusCode(500, new
                {
                    message = "Failed to mark notification as read.",
                    details = ex.Message
                });
            }
        }
    }
}
