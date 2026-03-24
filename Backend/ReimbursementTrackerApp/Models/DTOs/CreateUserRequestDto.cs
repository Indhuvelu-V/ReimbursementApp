using ReimbursementTrackerApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "Username name cannot be empty")]
        public string UserName { get; set; } = string.Empty;
        [Required] public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
       
        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(?:\+91|0)?\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits, with optional leading 0 or +91.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;
        [Required] public UserRole Role { get; set; }      // Enum
        [Required] public DepartmentType Department { get; set; } // Enum
     
      
    }
}

