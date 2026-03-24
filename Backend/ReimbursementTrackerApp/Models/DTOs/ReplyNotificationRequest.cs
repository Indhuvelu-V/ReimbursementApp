
using System.ComponentModel.DataAnnotations;

namespace ReimbursementTrackerApp.Models.DTOs
{
    public class ReplyNotificationRequestDto
    {
        [Required]
        public string NotificationId { get; set; } = string.Empty;

        [Required]
        public string Reply { get; set; } = string.Empty; // Employee reply
    }
}
