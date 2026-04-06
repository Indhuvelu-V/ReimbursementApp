
using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CheckUserRequestDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public string UserName { get; set; } = string.Empty;  // kept as UserName for frontend compatibility — actually UserId

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;
    }
}