using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDto?> CreateUser(CreateUserRequestDto request);
        Task<CreateUserResponseDto?> GetUserById(string userId);
        Task<PagedResponse<CreateUserResponseDto>> GetAllUsers(PaginationParams paginationParams);
        Task<CreateUserResponseDto> AssignManager(AssignManagerRequestDto request);
        Task<CreateUserResponseDto> UpdateUserStatus(UpdateUserStatusRequestDto request);
        Task<CreateUserResponseDto> UpdateMyProfile(string userId, UpdateMyProfileRequestDto request);
    }
}

