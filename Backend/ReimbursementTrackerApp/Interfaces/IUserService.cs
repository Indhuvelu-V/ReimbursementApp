using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IUserService
    {
     
        Task<CreateUserResponseDto?> CreateUser(CreateUserRequestDto request);
        Task<CreateUserResponseDto?> GetUserById(string userId);
       
        //Task<EmployeeDashboardDto?> GetEmployeeDashboard(string userId);
        Task<PagedResponse<CreateUserResponseDto>> GetAllUsers(PaginationParams paginationParams);

    }
}

