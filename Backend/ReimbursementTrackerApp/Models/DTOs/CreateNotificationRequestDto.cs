using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class CreateNotificationRequestDto
    {
        [Required] public string UserId { get; set; } = string.Empty; // Employee
        [Required] public string Message { get; set; } = string.Empty;// Manager message

        public string Description { get; set; } = string.Empty;
        public string SenderRole { get; set; } = "Manager";

    }
}
