using FluentAssertions;
using ReimbursementTrackerApp.Services;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _svc = new();

        [Fact]
        public void HashPassword_NewUser_ReturnsHashAndKey()
        {
            var hash = _svc.HashPassword("mypassword", null, out var key);

            hash.Should().NotBeNullOrEmpty();
            key.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void HashPassword_SamePasswordSameKey_ReturnsSameHash()
        {
            var hash1 = _svc.HashPassword("secret", null, out var key);
            var hash2 = _svc.HashPassword("secret", key, out _);

            hash1.Should().Equal(hash2);
        }

        [Fact]
        public void HashPassword_DifferentPasswords_ReturnsDifferentHashes()
        {
            var hash1 = _svc.HashPassword("password1", null, out var key);
            var hash2 = _svc.HashPassword("password2", key, out _);

            hash1.Should().NotEqual(hash2);
        }

        [Fact]
        public void HashPassword_ExistingKey_ReturnsNullOutKey()
        {
            _svc.HashPassword("pass", null, out var key);
            _svc.HashPassword("pass", key, out var outKey);

            outKey.Should().BeNull();
        }

        [Fact]
        public void HashPassword_EmptyPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _svc.HashPassword("", null, out _));
        }

        [Fact]
        public void HashPassword_NullPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                _svc.HashPassword(null!, null, out _));
        }

        [Fact]
        public void HashPassword_NewUser_OutKeyIsNotNull()
        {
            _svc.HashPassword("test123", null, out var key);
            key.Should().NotBeNull();
            key!.Length.Should().BeGreaterThan(0);
        }
    }
}
