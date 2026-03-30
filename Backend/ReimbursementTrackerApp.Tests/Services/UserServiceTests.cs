using FluentAssertions;
using Moq;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo = new();
        private readonly Mock<IPasswordService> _passwordService = new();
        private readonly Mock<IAuditLogService> _auditLogService = new();

        private UserService CreateService() =>
            new(_userRepo.Object, _passwordService.Object, _auditLogService.Object);

        // ── CreateUser ────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateUser_NewUser_ReturnsDto()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 1, 2, 3 });
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            _auditLogService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var svc = CreateService();
            var result = await svc.CreateUser(new CreateUserRequestDto
            {
                UserId = "U1", UserName = "Alice", Email = "a@b.com",
                Password = "pass123", Role = UserRole.Employee,
                Department = DepartmentType.IT
            });

            result.Should().NotBeNull();
            result!.UserId.Should().Be("U1");
            result.UserName.Should().Be("Alice");
        }

        [Fact]
        public async Task CreateUser_DuplicateUser_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<User> { new User { UserId = "U1" } });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateUser(new CreateUserRequestDto { UserId = "U1", Password = "x" }));
        }

        // ── GetUserById ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetUserById_ExistingUser_ReturnsDto()
        {
            _userRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<User> { new User { UserId = "U1", UserName = "Bob" } });
            _auditLogService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var svc = CreateService();
            var result = await svc.GetUserById("U1");

            result.Should().NotBeNull();
            result!.UserName.Should().Be("Bob");
        }

        [Fact]
        public async Task GetUserById_NotFound_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetUserById("MISSING"));
        }

        // ── GetAllUsers ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllUsers_WithRoleFilter_ReturnsOnlyMatchingRole()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "U1", UserName = "Alice", Role = UserRole.Employee },
                new User { UserId = "U2", UserName = "Bob",   Role = UserRole.Manager },
                new User { UserId = "U3", UserName = "Carol", Role = UserRole.Employee }
            });
            _auditLogService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var svc = CreateService();
            var result = await svc.GetAllUsers(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Role = "Employee"
            });

            result.Data.Should().HaveCount(2);
            result.Data.All(u => u.Role == UserRole.Employee).Should().BeTrue();
        }

        [Fact]
        public async Task GetAllUsers_WithNameFilter_ReturnsMatchingUsers()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { UserId = "U1", UserName = "Alice Smith" },
                new User { UserId = "U2", UserName = "Bob Jones" }
            });
            _auditLogService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var svc = CreateService();
            var result = await svc.GetAllUsers(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Name = "alice"
            });

            result.Data.Should().HaveCount(1);
            result.Data.First().UserName.Should().Be("Alice Smith");
        }

        [Fact]
        public async Task GetAllUsers_Pagination_ReturnsCorrectPage()
        {
            var users = Enumerable.Range(1, 15)
                .Select(i => new User { UserId = $"U{i}", UserName = $"User{i}" })
                .ToList();
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _auditLogService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var svc = CreateService();
            var result = await svc.GetAllUsers(new PaginationParams { PageNumber = 2, PageSize = 5 });

            result.Data.Should().HaveCount(5);
            result.TotalRecords.Should().Be(15);
            result.TotalPages.Should().Be(3);
        }
    }
}
