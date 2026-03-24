
using ReimbursementTrackerApp.Models.DTOs;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IAuthService
    {
        Task<CheckUserResponseDto> CheckUser(CheckUserRequestDto request);
    }
}