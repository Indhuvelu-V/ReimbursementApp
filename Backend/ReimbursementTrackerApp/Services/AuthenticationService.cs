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

            // 🔹 Find user by username
            var user = (users ?? Enumerable.Empty<User>())
                .FirstOrDefault(u => u.UserName == request.UserName);

            if (user == null)
                throw new UnAuthorizedException("Invalid username");

            // 🔹 Validate password
            var computedHash = _passwordService.HashPassword(
                request.Password,
                user.PasswordHash,
                out byte[]? newHash);

            if (!computedHash.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid password");

            // 🔹 Create token
            var tokenPayload = new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role
            };

            var token = _tokenService.CreateToken(tokenPayload);

            return new CheckUserResponseDto
            {
                //UserId = user.UserId,
                //UserName = user.UserName,
                //Role = user.Role,
                Token = token
            };
        }


    }
}