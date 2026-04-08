

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

            // 🔹 Check duplicate email
            var emailTaken = (users ?? Enumerable.Empty<User>())
                .Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
            if (emailTaken)
                throw new InvalidOperationException("Email already exists.");

            // 🔹 Check duplicate password (compare against each user's stored hash)
            var passwordDuplicate = (users ?? Enumerable.Empty<User>()).Any(u =>
            {
                byte[]? _;
                var hash = _passwordService.HashPassword(request.Password, u.PasswordHash, out _);
                return hash.SequenceEqual(u.Password);
            });
            if (passwordDuplicate)
                throw new InvalidOperationException("Password already in use. Please choose a different password.");

            // 🔹 Check duplicate phone
            var phoneTaken = (users ?? Enumerable.Empty<User>())
                .Any(u => u.Phone == request.Phone);
            if (phoneTaken)
                throw new InvalidOperationException("Mobile number already in use. Please use a different number.");

            // 🔹 Check duplicate account number
            if (!string.IsNullOrWhiteSpace(request.AccountNumber))
            {
                var accountTaken = (users ?? Enumerable.Empty<User>())
                    .Any(u => u.AccountNumber == request.AccountNumber);
                if (accountTaken)
                    throw new InvalidOperationException("Account number already in use. Please use a different account number.");
            }

            // 🔹 Enforce: only one Admin
            if (request.Role == UserRole.Admin)
            {
                var adminExists = (users ?? Enumerable.Empty<User>())
                    .Any(u => u.Role == UserRole.Admin);
                if (adminExists)
                    throw new InvalidOperationException("An Admin already exists. Only one Admin is allowed in the system.");
            }

            // 🔹 Enforce: only one Finance
            if (request.Role == UserRole.Finance)
            {
                var financeExists = (users ?? Enumerable.Empty<User>())
                    .Any(u => u.Role == UserRole.Finance);
                if (financeExists)
                    throw new InvalidOperationException("A Finance user already exists. Only one Finance role is allowed in the system.");
            }

            // 🔹 Enforce: only one Manager per department
            if (request.Role == UserRole.Manager)
            {
                var deptManagerExists = (users ?? Enumerable.Empty<User>())
                    .Any(u => u.Role == UserRole.Manager && u.Department == request.Department);

                if (deptManagerExists)
                    throw new InvalidOperationException($"A Manager already exists for the {request.Department} department. Please choose your role correctly.");
            }

            // 🔹 Enforce: only one TeamLead per department
            if (request.Role == UserRole.TeamLead)
            {
                var deptTeamLeadExists = (users ?? Enumerable.Empty<User>())
                    .Any(u => u.Role == UserRole.TeamLead && u.Department == request.Department);

                if (deptTeamLeadExists)
                    throw new InvalidOperationException($"A Team Lead already exists for the {request.Department} department.");
            }

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

            // 🔹 Auto-assign reporting manager based on role hierarchy:
            //    Employee  → same-dept TeamLead (their direct approver)
            //    TeamLead  → same-dept Manager  (their direct approver)
            //    Manager/Finance/Admin → no auto-assign
            string? managerId = null;
            if (request.Role == UserRole.Employee)
            {
                var deptTeamLead = (users ?? Enumerable.Empty<User>())
                    .FirstOrDefault(u => u.Role == UserRole.TeamLead && u.Department == request.Department);
                managerId = deptTeamLead?.UserId;
            }
            else if (request.Role == UserRole.TeamLead)
            {
                var deptManager = (users ?? Enumerable.Empty<User>())
                    .FirstOrDefault(u => u.Role == UserRole.Manager && u.Department == request.Department);
                managerId = deptManager?.UserId;
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
                ApprovalLevel = approvalLevel,
                ManagerId = managerId,
                BankName = request.BankName ?? string.Empty,
                AccountNumber = request.AccountNumber ?? string.Empty,
                IfscCode = !string.IsNullOrWhiteSpace(request.IfscCode) ? request.IfscCode.ToUpper() : string.Empty,
                BranchName = request.BranchName ?? string.Empty
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

            // 🔹 Resolve manager name for response
            string? managerName = null;
            if (!string.IsNullOrEmpty(user.ManagerId))
            {
                var mgr = (users ?? Enumerable.Empty<User>()).FirstOrDefault(u => u.UserId == user.ManagerId);
                managerName = mgr?.UserName;
            }

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel,
                ReportingManagerId = user.ManagerId,
                ReportingManagerName = managerName,
                BankName = user.BankName,
                AccountNumber = user.AccountNumber,
                IfscCode = user.IfscCode,
                BranchName = user.BranchName
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

            string? managerName = null;
            if (!string.IsNullOrEmpty(user.ManagerId))
            {
                var mgr = (users ?? Enumerable.Empty<User>()).FirstOrDefault(u => u.UserId == user.ManagerId);
                managerName = mgr?.UserName;
            }

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel,
                ReportingManagerId = user.ManagerId,
                ReportingManagerName = managerName,
                BankName = user.BankName,
                AccountNumber = user.AccountNumber,
                IfscCode = user.IfscCode,
                BranchName = user.BranchName
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
                    .Select(u =>
                    {
                        var mgr = !string.IsNullOrEmpty(u.ManagerId)
                            ? users.FirstOrDefault(m => m.UserId == u.ManagerId)
                            : null;
                        return new CreateUserResponseDto
                        {
                            UserId = u.UserId,
                            UserName = u.UserName,
                            Email = u.Email,
                            Role = u.Role,
                            Department = u.Department,
                            Status = u.Status,
                            ApprovalLevel = u.ApprovalLevel,
                            ReportingManagerId = u.ManagerId,
                            ReportingManagerName = mgr?.UserName
                        };
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


        // =====================================================
        // 4️⃣ ASSIGN MANAGER (Admin only — same department)
        // Supports assigning either a Manager or TeamLead as
        // the reporting approver for a user.
        // =====================================================
        public async Task<CreateUserResponseDto> AssignManager(AssignManagerRequestDto request)
        {
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            var employee = users.FirstOrDefault(u => u.UserId == request.EmployeeId)
                ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found.");

            var manager = users.FirstOrDefault(u => u.UserId == request.ManagerId)
                ?? throw new KeyNotFoundException($"Manager {request.ManagerId} not found.");

            if (manager.Role != UserRole.Manager && manager.Role != UserRole.TeamLead)
                throw new InvalidOperationException($"{manager.UserName} must be a Manager or Team Lead.");

            if (manager.Department != employee.Department)
                throw new InvalidOperationException(
                    $"{manager.UserName} belongs to {manager.Department} department. " +
                    $"You can only assign someone from the same department ({employee.Department}).");

            employee.ManagerId = manager.UserId;
            await _userRepo.UpdateAsync(employee.UserId, employee);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Admin assigned Manager {manager.UserId} to Employee {employee.UserId}",
                UserId = employee.UserId,
                UserName = employee.UserName,
                Role = employee.Role
            });

            return new CreateUserResponseDto
            {
                UserId = employee.UserId,
                UserName = employee.UserName,
                Email = employee.Email,
                Role = employee.Role,
                Department = employee.Department,
                Status = employee.Status,
                ApprovalLevel = employee.ApprovalLevel,
                ReportingManagerId = employee.ManagerId,
                ReportingManagerName = manager.UserName
            };
        }

        // =====================================================
        // 5️⃣ UPDATE USER STATUS (Admin only)
        // =====================================================
        public async Task<CreateUserResponseDto> UpdateUserStatus(UpdateUserStatusRequestDto request)
        {
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            var user = users.FirstOrDefault(u => u.UserId == request.UserId)
                ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

            user.Status = request.Status;
            await _userRepo.UpdateAsync(user.UserId, user);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"Admin updated status of User {user.UserId} to {request.Status}",
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role
            });

            var mgr = !string.IsNullOrEmpty(user.ManagerId)
                ? users.FirstOrDefault(m => m.UserId == user.ManagerId)
                : null;

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel,
                ReportingManagerId = user.ManagerId,
                ReportingManagerName = mgr?.UserName
            };
        }

        // =====================================================
        // 6️⃣ UPDATE MY PROFILE (any authenticated user)
        // =====================================================
        public async Task<CreateUserResponseDto> UpdateMyProfile(string userId, UpdateMyProfileRequestDto request)
        {
            var users = await _userRepo.GetAllAsync() ?? new List<User>();

            var user = users.FirstOrDefault(u => u.UserId == userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            // Validate duplicate phone if changed
            if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != user.Phone)
            {
                var phoneTaken = users.Any(u => u.UserId != userId && u.Phone == request.Phone);
                if (phoneTaken)
                    throw new InvalidOperationException("Mobile number already in use by another user.");
                user.Phone = request.Phone;
            }

            // Validate duplicate account number if changed
            if (!string.IsNullOrWhiteSpace(request.AccountNumber) && request.AccountNumber != user.AccountNumber)
            {
                var accountTaken = users.Any(u => u.UserId != userId && u.AccountNumber == request.AccountNumber);
                if (accountTaken)
                    throw new InvalidOperationException("Account number already in use by another user.");
                user.AccountNumber = request.AccountNumber;
            }

            if (!string.IsNullOrWhiteSpace(request.BankName)) user.BankName = request.BankName;
            if (!string.IsNullOrWhiteSpace(request.IfscCode)) user.IfscCode = request.IfscCode.ToUpper();
            if (!string.IsNullOrWhiteSpace(request.BranchName)) user.BranchName = request.BranchName;

            await _userRepo.UpdateAsync(user.UserId, user);

            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = $"User {userId} updated their profile",
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role
            });

            var mgr2 = !string.IsNullOrEmpty(user.ManagerId)
                ? users.FirstOrDefault(m => m.UserId == user.ManagerId)
                : null;

            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Department = user.Department,
                Status = user.Status,
                ApprovalLevel = user.ApprovalLevel,
                ReportingManagerId = user.ManagerId,
                ReportingManagerName = mgr2?.UserName,
                BankName = user.BankName,
                AccountNumber = user.AccountNumber,
                IfscCode = user.IfscCode,
                BranchName = user.BranchName
            };
        }
    }
}
