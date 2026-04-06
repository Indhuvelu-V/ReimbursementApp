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

        // ── AssignManager ─────────────────────────────────────────────────

        [Fact]
        public async Task AssignManager_ValidSameDepartment_Succeeds()
        {
            var employee = new User { UserId = "E1", UserName = "emp", Role = UserRole.Employee, Department = DepartmentType.IT };
            var manager  = new User { UserId = "M1", UserName = "mgr", Role = UserRole.Manager,  Department = DepartmentType.IT };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { employee, manager });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(employee);
            SetupAudit();

            var result = await CreateService().AssignManager(new AssignManagerRequestDto { EmployeeId = "E1", ManagerId = "M1" });

            result.ReportingManagerId.Should().Be("M1");
            result.ReportingManagerName.Should().Be("mgr");
        }

        [Fact]
        public async Task AssignManager_EmployeeNotFound_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().AssignManager(new AssignManagerRequestDto { EmployeeId = "MISSING", ManagerId = "M1" }));
        }

        [Fact]
        public async Task AssignManager_ManagerNotFound_ThrowsKeyNotFound()
        {
            var employee = new User { UserId = "E1", UserName = "emp", Role = UserRole.Employee, Department = DepartmentType.IT };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { employee });
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().AssignManager(new AssignManagerRequestDto { EmployeeId = "E1", ManagerId = "MISSING" }));
        }

        [Fact]
        public async Task AssignManager_UserIsNotManager_ThrowsInvalidOperation()
        {
            var employee = new User { UserId = "E1", UserName = "emp", Role = UserRole.Employee, Department = DepartmentType.IT };
            var notMgr   = new User { UserId = "M1", UserName = "notmgr", Role = UserRole.Employee, Department = DepartmentType.IT };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { employee, notMgr });
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().AssignManager(new AssignManagerRequestDto { EmployeeId = "E1", ManagerId = "M1" }));
        }

        [Fact]
        public async Task AssignManager_DifferentDepartment_ThrowsInvalidOperation()
        {
            var employee = new User { UserId = "E1", UserName = "emp", Role = UserRole.Employee, Department = DepartmentType.IT };
            var manager  = new User { UserId = "M1", UserName = "mgr", Role = UserRole.Manager,  Department = DepartmentType.HR };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { employee, manager });
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().AssignManager(new AssignManagerRequestDto { EmployeeId = "E1", ManagerId = "M1" }));
        }

        // ── UpdateUserStatus ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserStatus_ValidUser_UpdatesStatus()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, Status = UserStatus.Active };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            var result = await CreateService().UpdateUserStatus(new UpdateUserStatusRequestDto { UserId = "U1", Status = UserStatus.Inactive });
            result.Status.Should().Be(UserStatus.Inactive);
        }

        [Fact]
        public async Task UpdateUserStatus_UserNotFound_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().UpdateUserStatus(new UpdateUserStatusRequestDto { UserId = "MISSING", Status = UserStatus.Inactive }));
        }

        // ── UpdateMyProfile ───────────────────────────────────────────────

        [Fact]
        public async Task UpdateMyProfile_UpdatePhone_Succeeds()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, Phone = "1111111111" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            var result = await CreateService().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { Phone = "9999999999" });
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateMyProfile_DuplicatePhone_ThrowsInvalidOperation()
        {
            var user1 = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, Phone = "1111111111" };
            var user2 = new User { UserId = "U2", UserName = "bob",   Role = UserRole.Employee, Phone = "9999999999" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user1, user2 });
            SetupAudit();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { Phone = "9999999999" }));
        }

        [Fact]
        public async Task UpdateMyProfile_DuplicateAccountNumber_ThrowsInvalidOperation()
        {
            var user1 = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, AccountNumber = "111111111" };
            var user2 = new User { UserId = "U2", UserName = "bob",   Role = UserRole.Employee, AccountNumber = "999999999" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user1, user2 });
            SetupAudit();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateService().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { AccountNumber = "999999999" }));
        }

        [Fact]
        public async Task UpdateMyProfile_IfscCodeConvertedToUppercase()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, IfscCode = "HDFC0001234" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            await CreateService().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { IfscCode = "sbin0001234" });
            user.IfscCode.Should().Be("SBIN0001234");
        }

        [Fact]
        public async Task UpdateMyProfile_UserNotFound_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                CreateService().UpdateMyProfile("MISSING", new UpdateMyProfileRequestDto { Phone = "9999999999" }));
        }

        [Fact]
        public async Task UpdateMyProfile_SamePhone_SkipsDuplicateCheck()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, Phone = "9999999999" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            // Same phone — should not throw
            var result = await CreateService().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { Phone = "9999999999" });
            result.Should().NotBeNull();
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class UserServiceBranchTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<IAuditLogService>          _auditService    = new();

        private UserService Svc() => new(_userRepo.Object, _passwordService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupPassword() =>
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 1, 2, 3 });

        // Branch: Admin role → approvalLevel = null (default case)
        [Fact]
        public async Task CreateUser_AdminRole_ApprovalLevelNull()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "A1", UserName = "Admin", Password = "x", Role = UserRole.Admin });

            result!.ApprovalLevel.Should().BeNull();
        }

        // Branch: Employee role → approvalLevel = null
        [Fact]
        public async Task CreateUser_EmployeeRole_ApprovalLevelNull()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "E1", UserName = "Emp", Password = "x", Role = UserRole.Employee });

            result!.ApprovalLevel.Should().BeNull();
        }

        // Branch: hashKey is null → Array.Empty used
        [Fact]
        public async Task CreateUser_NullHashKey_UsesEmptyArray()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            byte[]? nullKey = null;
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out nullKey))
                .Returns(new byte[] { 1 });
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "U1", UserName = "Test", Password = "x", Role = UserRole.Employee });

            result.Should().NotBeNull();
        }

        // Branch: GetAllUsers — repo throws → wraps in Exception
        [Fact]
        public async Task GetAllUsers_RepoThrows_WrapsException()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("DB error"));
            SetupAudit();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                Svc().GetAllUsers(new PaginationParams { PageNumber = 1, PageSize = 10 }));

            ex.Message.Should().Contain("Unexpected error");
        }

        // Branch: GetAllUsers — no role/name filter, returns all
        [Fact]
        public async Task GetAllUsers_NoFilters_ReturnsAll()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "Alice" },
                new() { UserId = "U2", UserName = "Bob" }
            });
            SetupAudit();

            var result = await Svc().GetAllUsers(new PaginationParams { PageNumber = 1, PageSize = 10 });
            result.Data.Should().HaveCount(2);
        }

        // Branch: GetUserById — null users list → throws
        [Fact]
        public async Task GetUserById_NullUserList_ThrowsKeyNotFound()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<User>?)null);
            SetupAudit();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Svc().GetUserById("U1"));
        }
    }
}

namespace ReimbursementTrackerApp.Tests.Services
{
    public class UserServiceNewLogicTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<IAuditLogService>          _auditService    = new();

        private UserService Svc() => new(_userRepo.Object, _passwordService.Object, _auditService.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private void SetupPassword(byte[]? hash = null) =>
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
                .Returns(hash ?? new byte[] { 1, 2, 3 });

        // ── CreateUser: duplicate checks ──────────────────────────────────────

        [Fact]
        public async Task CreateUser_DuplicateEmail_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U2", Email = "a@b.com", Password = new byte[]{9}, PasswordHash = new byte[]{9}, Phone = "0000000000" }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 }); // won't match existing

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "U1", Email = "a@b.com", Password = "x", Phone = "1111111111" }));
        }

        [Fact]
        public async Task CreateUser_DuplicatePhone_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U2", Email = "other@b.com", Password = new byte[]{9}, PasswordHash = new byte[]{9}, Phone = "9999999999" }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "U1", Email = "new@b.com", Password = "x", Phone = "9999999999" }));
        }

        [Fact]
        public async Task CreateUser_DuplicateAccountNumber_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U2", Email = "other@b.com", Password = new byte[]{9}, PasswordHash = new byte[]{9}, Phone = "1111111111", AccountNumber = "ACC123" }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "U1", Email = "new@b.com", Password = "x", Phone = "2222222222", AccountNumber = "ACC123" }));
        }

        [Fact]
        public async Task CreateUser_DuplicatePassword_ThrowsInvalidOperation()
        {
            var existingHash = new byte[] { 1, 2, 3 };
            var existingUser = new User { UserId = "U2", Email = "other@b.com", Phone = "1111111111", Password = existingHash, PasswordHash = new byte[] { 4, 5, 6 } };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { existingUser });
            // HashPassword with existing user's salt returns same hash → duplicate
            _passwordService.Setup(p => p.HashPassword("samepass", existingUser.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(existingHash);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "U1", Email = "new@b.com", Password = "samepass", Phone = "2222222222" }));
        }

        // ── CreateUser: role enforcement ──────────────────────────────────────

        [Fact]
        public async Task CreateUser_SecondAdmin_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "A1", Role = UserRole.Admin, Email = "a@b.com", Phone = "1111111111", Password = new byte[]{9}, PasswordHash = new byte[]{9} }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "A2", Email = "new@b.com", Password = "x", Phone = "2222222222", Role = UserRole.Admin }));
        }

        [Fact]
        public async Task CreateUser_SecondFinance_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "F1", Role = UserRole.Finance, Email = "f@b.com", Phone = "1111111111", Password = new byte[]{9}, PasswordHash = new byte[]{9} }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "F2", Email = "new@b.com", Password = "x", Phone = "2222222222", Role = UserRole.Finance }));
        }

        [Fact]
        public async Task CreateUser_SecondManagerSameDept_ThrowsInvalidOperation()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "M1", Role = UserRole.Manager, Department = DepartmentType.IT, Email = "m@b.com", Phone = "1111111111", Password = new byte[]{9}, PasswordHash = new byte[]{9} }
            });
            _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Svc().CreateUser(new CreateUserRequestDto
                { UserId = "M2", Email = "new@b.com", Password = "x", Phone = "2222222222", Role = UserRole.Manager, Department = DepartmentType.IT }));
        }

        [Fact]
        public async Task CreateUser_ManagerDifferentDept_Succeeds()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "M1", Role = UserRole.Manager, Department = DepartmentType.IT, Email = "m@b.com", Phone = "1111111111", Password = new byte[]{9}, PasswordHash = new byte[]{9} }
            });
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "M2", Email = "new@b.com", Password = "x", Phone = "2222222222", Role = UserRole.Manager, Department = DepartmentType.HR });

            result.Should().NotBeNull();
            result!.Role.Should().Be(UserRole.Manager);
        }

        // ── CreateUser: auto-assign manager ──────────────────────────────────

        [Fact]
        public async Task CreateUser_EmployeeInDeptWithManager_AutoAssignsManager()
        {
            var mgr = new User { UserId = "M1", UserName = "Mgr", Role = UserRole.Manager, Department = DepartmentType.IT, Email = "m@b.com", Phone = "1111111111", Password = new byte[]{9}, PasswordHash = new byte[]{9} };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { mgr });
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "E1", Email = "e@b.com", Password = "x", Phone = "2222222222", Role = UserRole.Employee, Department = DepartmentType.IT });

            result!.ReportingManagerId.Should().Be("M1");
            result.ReportingManagerName.Should().Be("Mgr");
        }

        [Fact]
        public async Task CreateUser_ManagerRole_NoAutoAssign()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            { UserId = "M1", Email = "m@b.com", Password = "x", Phone = "1111111111", Role = UserRole.Manager, Department = DepartmentType.IT });

            result!.ReportingManagerId.Should().BeNull();
        }

        // ── CreateUser: bank details stored ──────────────────────────────────

        [Fact]
        public async Task CreateUser_WithBankDetails_StoresBankInfo()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            SetupPassword();
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            SetupAudit();

            var result = await Svc().CreateUser(new CreateUserRequestDto
            {
                UserId = "U1", Email = "u@b.com", Password = "x", Phone = "1111111111",
                Role = UserRole.Employee,
                BankName = "HDFC", AccountNumber = "123456", IfscCode = "hdfc0001", BranchName = "Main"
            });

            result!.BankName.Should().Be("HDFC");
            result.AccountNumber.Should().Be("123456");
            result.IfscCode.Should().Be("HDFC0001"); // uppercased
            result.BranchName.Should().Be("Main");
        }

        // ── GetUserById: manager name resolved ───────────────────────────────

        [Fact]
        public async Task GetUserById_WithManager_ResolvesManagerName()
        {
            var mgr = new User { UserId = "M1", UserName = "Manager One", Role = UserRole.Manager };
            var emp = new User { UserId = "E1", UserName = "Employee", Role = UserRole.Employee, ManagerId = "M1" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp, mgr });
            SetupAudit();

            var result = await Svc().GetUserById("E1");
            result!.ReportingManagerName.Should().Be("Manager One");
            result.ReportingManagerId.Should().Be("M1");
        }

        [Fact]
        public async Task GetUserById_NoManager_ManagerNameNull()
        {
            var emp = new User { UserId = "E1", UserName = "Employee", Role = UserRole.Employee, ManagerId = null };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp });
            SetupAudit();

            var result = await Svc().GetUserById("E1");
            result!.ReportingManagerName.Should().BeNull();
        }

        // ── UpdateMyProfile: bank fields updated ─────────────────────────────

        [Fact]
        public async Task UpdateMyProfile_UpdatesBankFields()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            var result = await Svc().UpdateMyProfile("U1", new UpdateMyProfileRequestDto
            {
                BankName = "SBI", AccountNumber = "999888777", IfscCode = "sbin0001", BranchName = "Branch A"
            });

            result.BankName.Should().Be("SBI");
            result.AccountNumber.Should().Be("999888777");
            result.IfscCode.Should().Be("SBIN0001");
            result.BranchName.Should().Be("Branch A");
        }

        [Fact]
        public async Task UpdateMyProfile_SameAccountNumber_SkipsDuplicateCheck()
        {
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, AccountNumber = "ACC123" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            // Same account number — should not throw
            var result = await Svc().UpdateMyProfile("U1", new UpdateMyProfileRequestDto { AccountNumber = "ACC123" });
            result.Should().NotBeNull();
        }

        // ── UpdateUserStatus: manager name in response ────────────────────────

        [Fact]
        public async Task UpdateUserStatus_WithManager_ResolvesManagerName()
        {
            var mgr = new User { UserId = "M1", UserName = "Manager One" };
            var user = new User { UserId = "U1", UserName = "alice", Role = UserRole.Employee, Status = UserStatus.Active, ManagerId = "M1" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user, mgr });
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).ReturnsAsync(user);
            SetupAudit();

            var result = await Svc().UpdateUserStatus(new UpdateUserStatusRequestDto { UserId = "U1", Status = UserStatus.Inactive });
            result.ReportingManagerName.Should().Be("Manager One");
        }

        // ── GetAllUsers: manager name resolved in paged result ────────────────

        [Fact]
        public async Task GetAllUsers_WithManager_ResolvesManagerName()
        {
            var mgr = new User { UserId = "M1", UserName = "Manager One", Role = UserRole.Manager };
            var emp = new User { UserId = "E1", UserName = "Employee", Role = UserRole.Employee, ManagerId = "M1" };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp, mgr });
            SetupAudit();

            var result = await Svc().GetAllUsers(new PaginationParams { PageNumber = 1, PageSize = 10 });
            var empDto = result.Data.First(u => u.UserId == "E1");
            empDto.ReportingManagerName.Should().Be("Manager One");
        }
    }
}
