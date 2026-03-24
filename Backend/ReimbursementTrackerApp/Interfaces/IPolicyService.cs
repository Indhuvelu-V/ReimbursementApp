using ReimbursementTrackerApp.Contexts;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IPolicyService
    {
        Task<IEnumerable<CreatePolicyResponseDto>> GetAllPoliciesAsync();
    }

}
