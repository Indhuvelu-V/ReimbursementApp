using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;
using ReimbursementTrackerApp.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class TokenServiceTests
    {
        private const string SecretKey = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!";

        private TokenService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Keys:Jwt", SecretKey }
                })
                .Build();
            return new TokenService(config);
        }

        private JwtSecurityToken DecodeToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }

        // ── CreateToken — basic ───────────────────────────────────────────────

        [Fact]
        public void CreateToken_ValidPayload_ReturnsNonEmptyString()
        {
            var result = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Employee
            });

            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void CreateToken_ReturnsValidJwtFormat()
        {
            var result = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Employee
            });

            // JWT has 3 parts separated by dots
            result.Split('.').Should().HaveCount(3);
        }

        // ── Claims in token ───────────────────────────────────────────────────

        [Fact]
        public void CreateToken_ContainsUserId()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Employee
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")
                ?.Value.Should().Be("U1");
        }

        [Fact]
        public void CreateToken_ContainsUserName()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Employee
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name")
                ?.Value.Should().Be("alice");
        }

        [Fact]
        public void CreateToken_ContainsRole()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Manager
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
                ?.Value.Should().Be("Manager");
        }

        [Fact]
        public void CreateToken_EmployeeRole_ClaimIsEmployee()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "bob", Role = UserRole.Employee
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
                ?.Value.Should().Be("Employee");
        }

        [Fact]
        public void CreateToken_AdminRole_ClaimIsAdmin()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "A1", UserName = "admin", Role = UserRole.Admin
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
                ?.Value.Should().Be("Admin");
        }

        [Fact]
        public void CreateToken_FinanceRole_ClaimIsFinance()
        {
            var token  = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "F1", UserName = "finance", Role = UserRole.Finance
            });
            var decoded = DecodeToken(token);

            decoded.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
                ?.Value.Should().Be("Finance");
        }

        // ── Expiry ────────────────────────────────────────────────────────────

        [Fact]
        public void CreateToken_ExpiresInOneDay()
        {
            var before = DateTime.UtcNow.AddHours(23);
            var after  = DateTime.UtcNow.AddHours(25);

            var token   = CreateService().CreateToken(new TokenPayloadDto
            {
                UserId = "U1", UserName = "alice", Role = UserRole.Employee
            });
            var decoded = DecodeToken(token);

            decoded.ValidTo.Should().BeAfter(before);
            decoded.ValidTo.Should().BeBefore(after);
        }

        // ── Different tokens for different users ──────────────────────────────

        [Fact]
        public void CreateToken_DifferentUsers_ReturnDifferentTokens()
        {
            var svc = CreateService();

            var token1 = svc.CreateToken(new TokenPayloadDto { UserId = "U1", UserName = "alice", Role = UserRole.Employee });
            var token2 = svc.CreateToken(new TokenPayloadDto { UserId = "U2", UserName = "bob",   Role = UserRole.Manager });

            token1.Should().NotBe(token2);
        }

        // ── Missing secret key ────────────────────────────────────────────────

        [Fact]
        public void Constructor_MissingSecretKey_ThrowsInvalidOperation()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            Assert.Throws<InvalidOperationException>(() => new TokenService(config));
        }
    }
}
