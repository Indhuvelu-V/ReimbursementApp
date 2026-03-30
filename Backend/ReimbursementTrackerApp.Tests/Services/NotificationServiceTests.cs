using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Services;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IRepository<string, Notification>> _notifRepo    = new();
        private readonly Mock<IAuditLogService>                   _auditService = new();
        private readonly Mock<ILogger<NotificationService>>       _logger       = new();

        private NotificationService CreateService() =>
            new(_notifRepo.Object, _auditService.Object, _logger.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        // ── CreateNotification ────────────────────────────────────────────────

        [Fact]
        public async Task CreateNotification_ValidRequest_ReturnsDto()
        {
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.CreateNotification(new CreateNotificationRequestDto
            {
                UserId     = "U1",
                Message    = "Your expense was approved.",
                SenderRole = "System"
            });

            result.Should().NotBeNull();
            result.Message.Should().Be("Your expense was approved.");
            result.UserId.Should().Be("U1");
            result.ReadStatus.Should().Be("Unread");
        }

        [Fact]
        public async Task CreateNotification_DefaultsSenderRoleToManager()
        {
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.CreateNotification(new CreateNotificationRequestDto
            {
                UserId  = "U1",
                Message = "Hello"
                // SenderRole not set → defaults to "Manager"
            });

            result.SenderRole.Should().Be("Manager");
        }

        // ── GetNotificationsByUser ────────────────────────────────────────────

        [Fact]
        public async Task GetNotificationsByUser_ReturnsOnlyUserNotifications()
        {
            var notifications = new List<Notification>
            {
                new Notification { NotificationId = "N1", UserId = "U1", Message = "Approved",  CreatedAt = DateTime.UtcNow },
                new Notification { NotificationId = "N2", UserId = "U2", Message = "Rejected",  CreatedAt = DateTime.UtcNow },
                new Notification { NotificationId = "N3", UserId = "U1", Message = "Paid",      CreatedAt = DateTime.UtcNow }
            };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(notifications);
            SetupAudit();

            var svc    = CreateService();
            var result = (await svc.GetNotificationsByUser("U1")).ToList();

            result.Should().HaveCount(2);
            result.All(n => n.UserId == "U1").Should().BeTrue();
        }

        [Fact]
        public async Task GetNotificationsByUser_NoNotifications_ReturnsEmpty()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>());
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.GetNotificationsByUser("U1");

            result.Should().BeEmpty();
        }

        // ── MarkAsRead ────────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAsRead_UnreadNotification_ChangesStatusToRead()
        {
            var notif = new Notification
            {
                NotificationId = "N1", UserId = "U1",
                Message = "Test", ReadStatus = "Unread"
            };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            _notifRepo.Setup(r => r.UpdateAsync("N1", It.IsAny<Notification>())).ReturnsAsync(notif);
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.MarkAsRead("N1", "U1");

            result.Should().NotBeNull();
            result!.ReadStatus.Should().Be("Read");
        }

        [Fact]
        public async Task MarkAsRead_WrongUser_ReturnsNull()
        {
            var notif = new Notification
            {
                NotificationId = "N1", UserId = "U1", ReadStatus = "Unread"
            };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.MarkAsRead("N1", "WRONG_USER");

            result.Should().BeNull();
        }

        // ── ReplyNotification ─────────────────────────────────────────────────

        [Fact]
        public async Task ReplyNotification_ManagerSent_SavesReply()
        {
            var notif = new Notification
            {
                NotificationId = "N1", UserId = "U1",
                Message = "Please clarify", SenderRole = "Manager", ReadStatus = "Unread"
            };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            _notifRepo.Setup(r => r.UpdateAsync("N1", It.IsAny<Notification>())).ReturnsAsync(notif);
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.ReplyNotification(
                new ReplyNotificationRequestDto { NotificationId = "N1", Reply = "Clarified!" },
                "U1");

            result.Should().NotBeNull();
            result!.Reply.Should().Be("Clarified!");
            result.ReadStatus.Should().Be("Read");
        }

        [Fact]
        public async Task ReplyNotification_SystemSent_ReturnsNull()
        {
            var notif = new Notification
            {
                NotificationId = "N1", UserId = "U1",
                Message = "Auto message", SenderRole = "System"
            };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            SetupAudit();

            var svc    = CreateService();
            var result = await svc.ReplyNotification(
                new ReplyNotificationRequestDto { NotificationId = "N1", Reply = "Reply" },
                "U1");

            result.Should().BeNull();
        }
    }
}
