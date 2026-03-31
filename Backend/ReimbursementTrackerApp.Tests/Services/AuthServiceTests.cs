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
    }
}
