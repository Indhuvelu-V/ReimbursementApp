using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Services;
using Xunit;

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

        private void SetupAdd() =>
            _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

        [Fact]
        public async Task CreateNotification_ValidRequest_ReturnsDto()
        {
            SetupAdd(); SetupAudit();
            var result = await CreateService().CreateNotification(new CreateNotificationRequestDto
            {
                UserId = "U1", Message = "Expense approved.", SenderRole = "System"
            });
            result.Should().NotBeNull();
            result.Message.Should().Be("Expense approved.");
            result.UserId.Should().Be("U1");
            result.ReadStatus.Should().Be("Unread");
        }

        [Fact]
        public async Task CreateNotification_NoSenderRole_DefaultsToManager()
        {
            SetupAdd(); SetupAudit();
            var result = await CreateService().CreateNotification(new CreateNotificationRequestDto
            {
                UserId = "U1", Message = "Hello"
            });
            result.SenderRole.Should().Be("Manager");
        }

        [Fact]
        public async Task CreateNotification_SetsCreatedAtToUtcNow()
        {
            SetupAdd(); SetupAudit();
            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = await CreateService().CreateNotification(new CreateNotificationRequestDto
            {
                UserId = "U1", Message = "Test"
            });
            result.CreatedAt.Should().BeAfter(before);
        }

        [Fact]
        public async Task CreateNotification_AuditLogFails_StillReturnsDto()
        {
            SetupAdd();
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ThrowsAsync(new Exception("Audit failure"));
            var result = await CreateService().CreateNotification(new CreateNotificationRequestDto
            {
                UserId = "U1", Message = "Test"
            });
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetNotificationsByUser_ReturnsOnlyUserNotifications()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>
            {
                new() { NotificationId = "N1", UserId = "U1", Message = "Approved", CreatedAt = DateTime.UtcNow },
                new() { NotificationId = "N2", UserId = "U2", Message = "Rejected", CreatedAt = DateTime.UtcNow },
                new() { NotificationId = "N3", UserId = "U1", Message = "Paid",     CreatedAt = DateTime.UtcNow }
            });
            SetupAudit();
            var result = (await CreateService().GetNotificationsByUser("U1")).ToList();
            result.Should().HaveCount(2);
            result.All(n => n.UserId == "U1").Should().BeTrue();
        }

        [Fact]
        public async Task GetNotificationsByUser_NoNotifications_ReturnsEmpty()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>());
            SetupAudit();
            var result = await CreateService().GetNotificationsByUser("U1");
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetNotificationsByUser_OrderedNewestFirst()
        {
            var older = DateTime.UtcNow.AddHours(-2);
            var newer = DateTime.UtcNow;
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>
            {
                new() { NotificationId = "N1", UserId = "U1", Message = "Old", CreatedAt = older },
                new() { NotificationId = "N2", UserId = "U1", Message = "New", CreatedAt = newer }
            });
            SetupAudit();
            var result = (await CreateService().GetNotificationsByUser("U1")).ToList();
            result.First().NotificationId.Should().Be("N2");
        }

        [Fact]
        public async Task MarkAsRead_UnreadNotification_ChangesStatusToRead()
        {
            var notif = new Notification { NotificationId = "N1", UserId = "U1", Message = "Test", ReadStatus = "Unread" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            _notifRepo.Setup(r => r.UpdateAsync("N1", It.IsAny<Notification>())).ReturnsAsync(notif);
            SetupAudit();
            var result = await CreateService().MarkAsRead("N1", "U1");
            result.Should().NotBeNull();
            result!.ReadStatus.Should().Be("Read");
        }

        [Fact]
        public async Task MarkAsRead_AlreadyRead_ReturnsWithoutUpdate()
        {
            var notif = new Notification { NotificationId = "N1", UserId = "U1", ReadStatus = "Read" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            SetupAudit();
            var result = await CreateService().MarkAsRead("N1", "U1");
            result.Should().NotBeNull();
            _notifRepo.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsRead_WrongUser_ReturnsNull()
        {
            var notif = new Notification { NotificationId = "N1", UserId = "U1", ReadStatus = "Unread" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            SetupAudit();
            var result = await CreateService().MarkAsRead("N1", "WRONG_USER");
            result.Should().BeNull();
        }

        [Fact]
        public async Task MarkAsRead_NotFound_ReturnsNull()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>());
            SetupAudit();
            var result = await CreateService().MarkAsRead("MISSING", "U1");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ReplyNotification_ValidReply_SavesReplyAndMarksRead()
        {
            var notif = new Notification { NotificationId = "N1", UserId = "U1", Message = "Clarify?", SenderRole = "Manager", ReadStatus = "Unread" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            _notifRepo.Setup(r => r.UpdateAsync("N1", It.IsAny<Notification>())).ReturnsAsync(notif);
            SetupAudit();
            var result = await CreateService().ReplyNotification(
                new ReplyNotificationRequestDto { NotificationId = "N1", Reply = "Clarified!" }, "U1");
            result.Should().NotBeNull();
            result!.Reply.Should().Be("Clarified!");
            result.ReadStatus.Should().Be("Read");
        }

        [Fact]
        public async Task ReplyNotification_NotificationNotFound_ReturnsNull()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>());
            SetupAudit();
            var result = await CreateService().ReplyNotification(
                new ReplyNotificationRequestDto { NotificationId = "MISSING", Reply = "Reply" }, "U1");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ReplyNotification_WrongUser_ReturnsNull()
        {
            var notif = new Notification { NotificationId = "N1", UserId = "U1", SenderRole = "Manager" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            SetupAudit();
            var result = await CreateService().ReplyNotification(
                new ReplyNotificationRequestDto { NotificationId = "N1", Reply = "Reply" }, "WRONG");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSentNotifications_ReturnsSenderNotifications()
        {
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>
            {
                new() { NotificationId = "N1", UserId = "U1", SenderId = "MGR1", CreatedAt = DateTime.UtcNow },
                new() { NotificationId = "N2", UserId = "U2", SenderId = "MGR1", CreatedAt = DateTime.UtcNow },
                new() { NotificationId = "N3", UserId = "U3", SenderId = "MGR2", CreatedAt = DateTime.UtcNow }
            });
            var result = (await CreateService().GetSentNotifications("MGR1")).ToList();
            result.Should().HaveCount(2);
            result.All(n => n.SenderId == "MGR1").Should().BeTrue();
        }

        [Fact]
        public async Task MarkSentAsRead_ValidSender_UpdatesReadStatus()
        {
            var notif = new Notification { NotificationId = "N1", SenderId = "MGR1", ReadStatus = "Unread" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            _notifRepo.Setup(r => r.UpdateAsync("N1", It.IsAny<Notification>())).ReturnsAsync(notif);
            await CreateService().MarkSentAsRead("N1", "MGR1");
            _notifRepo.Verify(r => r.UpdateAsync("N1", It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task MarkSentAsRead_WrongSender_DoesNotUpdate()
        {
            var notif = new Notification { NotificationId = "N1", SenderId = "MGR1", ReadStatus = "Unread" };
            _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification> { notif });
            await CreateService().MarkSentAsRead("N1", "WRONG");
            _notifRepo.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<Notification>()), Times.Never);
        }
    }
}
