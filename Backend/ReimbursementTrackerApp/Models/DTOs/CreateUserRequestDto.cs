using ReimbursementTrackerApp.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "Username name cannot be empty")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email must be a valid @gmail.com address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(?:\+91|0)?\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits, with optional leading 0 or +91.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        // Nullable — Admin and Finance cover all departments, no specific dept needed
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DepartmentType? Department { get; set; }

        // Bank Details
        [Required(ErrorMessage = "Bank name is required.")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account number is required.")]
        [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Account number must be 9–18 digits.")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "IFSC code is required.")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code format.")]
        public string IfscCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Branch name is required.")]
        public string BranchName { get; set; } = string.Empty;
    }
}
