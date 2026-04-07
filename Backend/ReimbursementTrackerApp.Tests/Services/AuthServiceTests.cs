using FluentAssertions;
using Moq;
using ReimbursementTrackerApp.Exceptions;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<ITokenService>             _tokenService    = new();

        private AuthService CreateService() =>
            new(_userRepo.Object, _passwordService.Object, _tokenService.Object);

        private User MakeUser(string userName = "alice", UserStatus status = UserStatus.Active) =>
            new()
            {
                UserId       = "U1",
                UserName     = userName,
                Role         = UserRole.Employee,
                Password     = new byte[] { 1, 2, 3 },
                PasswordHash = new byte[] { 4, 5, 6 },
                Status       = status
            };

        // ── CheckUser ─────────────────────────────────────────────────────────

        [Fact]
        public async Task CheckUser_ValidCredentials_ReturnsToken()
        {
            var user = MakeUser("alice");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _passwordService
                .Setup(p => p.HashPassword("pass123", user.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(user.Password);
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("mock-jwt");

            var result = await CreateService().CheckUser(new CheckUserRequestDto
            {
                UserName = "alice", Password = "pass123"
            });

            result.Should().NotBeNull();
            result.Token.Should().Be("mock-jwt");
        }

        [Fact]
        public async Task CheckUser_WrongPassword_ThrowsUnAuthorized()
        {
            var user = MakeUser("alice");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _passwordService
                .Setup(p => p.HashPassword(It.IsAny<string>(), user.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99, 99, 99 });

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                CreateService().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "wrong" }));
        }

        [Fact]
        public async Task CheckUser_UserNotFound_ThrowsUnAuthorized()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                CreateService().CheckUser(new CheckUserRequestDto { UserName = "nobody", Password = "x" }));
        }

        [Fact]
        public async Task CheckUser_CaseSensitiveUserName_ThrowsUnAuthorized()
        {
            var user = MakeUser("Alice");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                CreateService().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass" }));
        }

        [Fact]
        public async Task CheckUser_ValidCredentials_TokenPayloadContainsCorrectUserInfo()
        {
            var user = MakeUser("bob");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
            _passwordService
                .Setup(p => p.HashPassword("secret", user.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(user.Password);

            TokenPayloadDto? captured = null;
            _tokenService
                .Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                .Callback<TokenPayloadDto>(p => captured = p)
                .Returns("token-xyz");

            await CreateService().CheckUser(new CheckUserRequestDto { UserName = "bob", Password = "secret" });

            captured.Should().NotBeNull();
            captured!.UserId.Should().Be("U1");
            captured.UserName.Should().Be("bob");
            captured.Role.Should().Be(UserRole.Employee);
        }

        [Fact]
        public async Task CheckUser_EmptyUserList_ThrowsUnAuthorized()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<User>?)null);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                CreateService().CheckUser(new CheckUserRequestDto { UserName = "x", Password = "y" }));
        }

        // ── Multi-user same username ───────────────────────────────────────

        [Fact]
        public async Task CheckUser_MultipleUsersWithSameName_MatchesByPassword()
        {
            var user1 = MakeUser("alice");
            var user2 = new User
            {
                UserId = "U2", UserName = "alice", Role = UserRole.Manager,
                Password = new byte[] { 9, 8, 7 }, PasswordHash = new byte[] { 6, 5, 4 }
            };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user1, user2 });

            // user1 password does NOT match
            _passwordService
                .Setup(p => p.HashPassword("pass_u2", user1.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99, 99 });
            // user2 password matches
            _passwordService
                .Setup(p => p.HashPassword("pass_u2", user2.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(user2.Password);

            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("tok-u2");

            var result = await CreateService().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass_u2" });
            result.Token.Should().Be("tok-u2");
        }

        [Fact]
        public async Task CheckUser_MultipleUsersWithSameName_WrongPassword_ThrowsUnAuthorized()
        {
            var user1 = MakeUser("alice");
            var user2 = new User
            {
                UserId = "U2", UserName = "alice", Role = UserRole.Manager,
                Password = new byte[] { 9, 8, 7 }, PasswordHash = new byte[] { 6, 5, 4 }
            };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user1, user2 });

            _passwordService
                .Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99, 99, 99 });

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                CreateService().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "wrong" }));
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class AuthServiceBranchTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<ITokenService>             _tokenService    = new();

        private AuthService Svc() => new(_userRepo.Object, _passwordService.Object, _tokenService.Object);

        // Branch: users is null → Enumerable.Empty used → user not found
        [Fact]
        public async Task CheckUser_NullUserList_ThrowsUnAuthorized()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<User>?)null);
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                Svc().CheckUser(new CheckUserRequestDto { UserName = "x", Password = "y" }));
        }

        // Branch: multiple users, correct one found by exact username match
        [Fact]
        public async Task CheckUser_MultipleUsers_MatchesCorrectOne()
        {
            var target = new User { UserId = "U2", UserName = "bob", Role = UserRole.Manager, Password = new byte[] { 5 }, PasswordHash = new byte[] { 6 } };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new() { UserId = "U1", UserName = "alice", Password = new byte[] { 1 }, PasswordHash = new byte[] { 2 } },
                target
            });
            _passwordService.Setup(p => p.HashPassword("pass", target.PasswordHash, out It.Ref<byte[]?>.IsAny)).Returns(target.Password);
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("tok");

            var result = await Svc().CheckUser(new CheckUserRequestDto { UserName = "bob", Password = "pass" });
            result.Token.Should().Be("tok");
        }
    }
}

namespace ReimbursementTrackerApp.Tests.Services
{
    public class AuthServiceNewLogicTests
    {
        private readonly Mock<IRepository<string, User>> _userRepo        = new();
        private readonly Mock<IPasswordService>          _passwordService = new();
        private readonly Mock<ITokenService>             _tokenService    = new();

        private AuthService Svc() => new(_userRepo.Object, _passwordService.Object, _tokenService.Object);

        private User MakeUser(string id, string name, byte[] pwd, byte[] salt, string? managerId = null) =>
            new() { UserId = id, UserName = name, Role = UserRole.Employee, Password = pwd, PasswordHash = salt, ManagerId = managerId };

        // ── Manager name/id resolved in response ──────────────────────────────

        [Fact]
        public async Task CheckUser_WithManager_ReturnsManagerNameAndId()
        {
            var mgr = new User { UserId = "M1", UserName = "Manager One", Role = UserRole.Manager, Password = new byte[]{9}, PasswordHash = new byte[]{9} };
            var emp = MakeUser("E1", "alice", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, managerId: "M1");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp, mgr });
            _passwordService.Setup(p => p.HashPassword("pass", emp.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(emp.Password);
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("tok");

            var result = await Svc().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass" });

            result.ReportingManagerName.Should().Be("Manager One");
            result.ReportingManagerId.Should().Be("M1");
        }

        [Fact]
        public async Task CheckUser_NoManager_ManagerNameAndIdNull()
        {
            var emp = MakeUser("E1", "alice", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, managerId: null);
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp });
            _passwordService.Setup(p => p.HashPassword("pass", emp.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(emp.Password);
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("tok");

            var result = await Svc().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass" });

            result.ReportingManagerName.Should().BeNull();
            result.ReportingManagerId.Should().BeNull();
        }

        [Fact]
        public async Task CheckUser_ManagerIdSetButManagerNotFound_ManagerNameNull()
        {
            // ManagerId set but no matching user in list
            var emp = MakeUser("E1", "alice", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }, managerId: "MISSING_MGR");
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { emp });
            _passwordService.Setup(p => p.HashPassword("pass", emp.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(emp.Password);
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("tok");

            var result = await Svc().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass" });

            result.ReportingManagerName.Should().BeNull();
            result.ReportingManagerId.Should().BeNull();
        }

        // ── Token payload correctness ─────────────────────────────────────────

        [Fact]
        public async Task CheckUser_TokenPayload_HasCorrectRole()
        {
            var mgr = new User
            {
                UserId = "M1", UserName = "mgr", Role = UserRole.Manager,
                Password = new byte[] { 5, 6, 7 }, PasswordHash = new byte[] { 8, 9, 10 }
            };
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { mgr });
            _passwordService.Setup(p => p.HashPassword("pass", mgr.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(mgr.Password);

            TokenPayloadDto? captured = null;
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                .Callback<TokenPayloadDto>(p => captured = p)
                .Returns("tok");

            await Svc().CheckUser(new CheckUserRequestDto { UserName = "mgr", Password = "pass" });

            captured!.Role.Should().Be(UserRole.Manager);
            captured.UserId.Should().Be("M1");
        }

        // ── Same username, first user matches ─────────────────────────────────

        [Fact]
        public async Task CheckUser_SameUsername_FirstUserMatches_ReturnsFirstUserToken()
        {
            var u1 = MakeUser("U1", "alice", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });
            var u2 = MakeUser("U2", "alice", new byte[] { 7, 8, 9 }, new byte[] { 10, 11, 12 });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { u1, u2 });

            // u1 password matches
            _passwordService.Setup(p => p.HashPassword("pass_u1", u1.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(u1.Password);
            // u2 password does NOT match
            _passwordService.Setup(p => p.HashPassword("pass_u1", u2.PasswordHash, out It.Ref<byte[]?>.IsAny))
                .Returns(new byte[] { 99 });

            TokenPayloadDto? captured = null;
            _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                .Callback<TokenPayloadDto>(p => captured = p)
                .Returns("tok-u1");

            var result = await Svc().CheckUser(new CheckUserRequestDto { UserName = "alice", Password = "pass_u1" });

            result.Token.Should().Be("tok-u1");
            captured!.UserId.Should().Be("U1");
        }

        // ── Edge: empty username ──────────────────────────────────────────────

        [Fact]
        public async Task CheckUser_EmptyUsername_ThrowsUnAuthorized()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                MakeUser("U1", "alice", new byte[]{1}, new byte[]{2})
            });

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                Svc().CheckUser(new CheckUserRequestDto { UserName = "", Password = "pass" }));
        }
    }
}
