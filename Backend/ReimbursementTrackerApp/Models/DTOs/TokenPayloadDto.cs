
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class TokenPayloadDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
       

        public UserRole Role { get; set; }    // Enum
           
        
    }
}