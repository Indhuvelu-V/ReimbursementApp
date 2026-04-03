

using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<string, User> _userRepo;
        private readonly IPasswordService _passwordService;
        private readonly IAuditLogService _auditLogService;

        public UserService(
            IRepository<string, User> userRepo,
            IPasswordService passwordService,
            IAuditLogService auditLogService)
        {
            _userRepo = userRepo;
            _passwordService = passwordService;
            _auditLogService = auditLogService;
        }

        // =====================================================
        // 1️⃣ CREATE USER
        // =====================================================
        public async Task<CreateUserResponseDto?> CreateUser(CreateUserRequestDto request)
        {
            var users = await _userRepo.GetAllAsync();
            var existingUser = (users ?? Enumerable.Empty<User>()).FirstOrDefault(u => u.UserId == request.UserId);


            if (existingUser != null)
                throw new InvalidOperationException("User already exists.");

            byte[]? hashKey;
            var passwordHash = _passwordService.HashPassword(request.Password, null, out hashKey);

            ApprovalLevel? approvalLevel = null;

            switch (request.Role)
            {
                case UserRole.Manager:
                    approvalLevel = ApprovalLevel.Level1;
                    break;

                case UserRole.Finance:
                    approvalLevel = ApprovalLevel.Finance;
                    break;

                default:
                    approvalLevel = null;
                    break;
            }

            var user = new User
            {
                UserId = request.UserId,
                UserName = request.UserName,
                Email = request.Email,
                Role = request.Role,
                Department = request.Department,
                Phone = request.Phone,
                Password = passwordHash,
                PasswordHash = hashKey ?? Array.Empty<byte>(),
                Status = UserStatus.Active,
                ApprovalLevel = approvalLevel
            };

            await _userRepo.AddAsync(user);

            // 🔹 Audit Log
            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Created User {user.UserId}",
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role
            });

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel
            };
        }

        // =====================================================
        // 2️⃣ GET USER BY ID
        // =====================================================
        public async Task<CreateUserResponseDto?> GetUserById(string userId)
        {
            var users = await _userRepo.GetAllAsync();
            var user = users?.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                throw new KeyNotFoundException($"User {userId} not found.");

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Fetched User {userId}"
            });

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel
            };
        }

        // =====================================================
        // 3️⃣ GET ALL USERS
        // =====================================================
        public async Task<PagedResponse<CreateUserResponseDto>> GetAllUsers(PaginationParams paginationParams)
        {
            try
            {
                var users = await _userRepo.GetAllAsync() ?? new List<User>();

                var query = users.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(paginationParams.Role))
                    query = query.Where(u => u.Role.ToString().Equals(paginationParams.Role, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(paginationParams.Name))
                    query = query.Where(u => u.UserName.Contains(paginationParams.Name, StringComparison.OrdinalIgnoreCase));

                var filtered = query.OrderBy(u => u.UserId).ToList();
                var totalRecords = filtered.Count;

                var pagedUsers = filtered
                    .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize)
                    .Select(u => new CreateUserResponseDto
                    {
                        UserId = u.UserId,
                        UserName = u.UserName,
                        Email = u.Email,
                        Role = u.Role,
                        Department = u.Department,
                        Status = u.Status,
                        ApprovalLevel = u.ApprovalLevel
                    })
                    .ToList();

                await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
                {
                    Action = $"Fetched all users. Count: {pagedUsers.Count}"
                });

                return new PagedResponse<CreateUserResponseDto>(
                    pagedUsers, totalRecords, paginationParams.PageNumber, paginationParams.PageSize);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while fetching users.", ex);
            }
        }


    }
}
