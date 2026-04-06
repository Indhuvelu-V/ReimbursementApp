using ReimbursementTrackerApp.Exceptions;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<string, User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;

        public AuthService(
            IRepository<string, User> userRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task<CheckUserResponseDto> CheckUser(CheckUserRequestDto request)
        {
            // 🔹 Get all users
            var users = await _userRepository.GetAllAsync();

            // 🔹 Find all users with matching username (same name allowed)
            var matchingUsers = (users ?? Enumerable.Empty<User>())
                .Where(u => u.UserName == request.UserName)
                .ToList();

            if (!matchingUsers.Any())
                throw new UnAuthorizedException("User not found. Please register first.");

            // 🔹 Among matching usernames, find the one whose password matches
            User? user = null;
            foreach (var candidate in matchingUsers)
            {
                var hash = _passwordService.HashPassword(request.Password, candidate.PasswordHash, out _);
                if (hash.SequenceEqual(candidate.Password))
                {
                    user = candidate;
                    break;
                }
            }

            if (user == null)
                throw new UnAuthorizedException("Invalid password. Please register first if you don't have an account.");

            // 🔹 Validate password
            var computedHash = _passwordService.HashPassword(
                request.Password,
                user.PasswordHash,
                out byte[]? newHash);

            if (!computedHash.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid password. Please register first if you don't have an account.");

            // 🔹 Create token
            var tokenPayload = new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role
            };

            var token = _tokenService.CreateToken(tokenPayload);

            // 🔹 Find reporting manager
            string? managerName = null;
            string? managerId = null;
            if (!string.IsNullOrEmpty(user.ManagerId))
            {
                var manager = (users ?? Enumerable.Empty<User>())
                    .FirstOrDefault(u => u.UserId == user.ManagerId);
                managerName = manager?.UserName;
                managerId = manager?.UserId;
            }

            return new CheckUserResponseDto
            {
                Token = token,
                ReportingManagerName = managerName,
                ReportingManagerId = managerId
            };
        }


    }
}