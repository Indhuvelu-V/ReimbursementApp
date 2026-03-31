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
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<IAuditLogService>          _auditService    = new();

        private UserService CreateService() =>
            new(_userRepo.Object, _passwordService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupPassword() =>
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 1, 2, 3 });

        // ── CreateUser ────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateUser_NewUser_ReturnsDto()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await CreateService().CreateUser(new CreateUserRequestDto
            {
                UserId = "U1", UserName = "Alice", Email = "a@b.com",
                Password = "pass123", Role = UserRole.Employee, Department = DepartmentType.IT
            });

            result.Should().NotBeNull();
            result!.UserId.Should().Be("U1");
            result.UserName.Should().Be("Alice");
            result.Role.Should().Be(UserRole.Employee);
        }

        [Fact]
        public async Task CreateUser_ManagerRole_SetsApprovalLevelOne()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await CreateService().CreateUser(new CreateUserRequestDto
            {
                UserId = "M1", UserName = "Mgr", Password = "x", Role = UserRole.Manager
            });

            result!.ApprovalLevel.Should().Be(ApprovalLevel.Level1);
        }

        [Fact]
        public async Task CreateUser_FinanceRole_SetsApprovalLevelFinance()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await CreateService().CreateUser(new CreateUserRequestDto
            {
                UserId = "F1", UserName = "Fin", Password = "x", Role = UserRole.Finance
            });

            result!.ApprovalLevel.Should().Be(ApprovalLevel.Finance);
        }

        [Fact]
        public async Task CreateUser_DuplicateUserId_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<User> { new() { UserId = "U1" } });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().CreateUser(new CreateUserRequestDto { UserId = "U1", Password = "x" }));
        }

        [Fact]
        public async Task CreateUser_SetsStatusToActive()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await CreateService().CreateUser(new CreateUserRequestDto
            {
                UserId = "U2", UserName = "Bob", Password = "x", Role = UserRole.Employee
            });

            result!.Status.Should().Be(UserStatus.Active);
        }

        // ── GetUserById ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetUserById_ExistingUser_ReturnsDto()
        {
            _userRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<User> { new() { UserId = "U1", UserName = "Bob", Role = UserRole.Employee } });
            SetupAudit();

            var result = await CreateService().GetUserById("U1");
            result.Should().NotBeNull();
            result!.UserName.Should().Be("Bob");
        }

        [Fact]
        public async Task GetUserById_NotFound_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupAudit();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().GetUserById("MISSING"));
        }

        // ── GetAllUsers ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllUsers_NoFilter_ReturnsAll()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice", Role = UserRole.Employee },
                new() { UserId = "U2", UserName = "Bob",   Role = UserRole.Manager }
            });
            SetupAudit();

            var result = await CreateService().GetAllUsers(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(2);
            result.TotalRecords.Should().Be(2);
        }

        [Fact]
        public async Task GetAllUsers_RoleFilter_ReturnsOnlyMatchingRole()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice", Role = UserRole.Employee },
                new() { UserId = "U2", UserName = "Bob",   Role = UserRole.Manager },
                new() { UserId = "U3", UserName = "Carol", Role = UserRole.Employee }
            });
            SetupAudit();

            var result = await CreateService().GetAllUsers(new PaginationParams
            {
                PageNumber = 1, PageSize = 10, Role = "Employee"
            });

            result.Data.Should().HaveCount(2);
            result.Data.All(u => u.Role == UserRole.Employee).Should().BeTrue();
        }

        [Fact]
        public async Task GetAllUsers_NameFilter_ReturnsMatchingUsers()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice Smith" },
                new() { UserId = "U2", UserName = "Bob Jones" }
            });
            SetupAudit();

            var result = await CreateService().GetAllUsers(new PaginationParams
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
                .Select(i => new User { UserId = $"U{i:D2}", UserName = $"User{i}" })
                .ToList();
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            SetupAudit();

            var result = await CreateService().GetAllUsers(new PaginationParams { PageNumber = 2, PageSize = 5 });
            result.Data.Should().HaveCount(5);
            result.TotalRecords.Should().Be(15);
            result.TotalPages.Should().Be(3);
        }
    }
}
