

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Security.Claims;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // =====================================================
        // 🔹 1️⃣ REGISTER USER (Anyone)
        // =====================================================

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CreateUserRequestDto request)
        {
            try
            {
                var result = await _userService.CreateUser(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        // =====================================================
        // 🔹 2️⃣ GET USER BY ID (Any authenticated user)
        // =====================================================
        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            try
            {
                var result = await _userService.GetUserById(userId);
                if (result == null)
                    return NotFound("User not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // =====================================================
        // 🔹 3️⃣ GET ALL USERS (Admin Only)
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpGet("allusers")]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var users = await _userService.GetAllUsers(paginationParams);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch users.", details = ex.Message });
            }
        }

        // =====================================================
        // 🔹 4️⃣ ASSIGN MANAGER (Admin Only)
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("assign-manager")]
        public async Task<IActionResult> AssignManager([FromBody] AssignManagerRequestDto request)
        {
            try
            {
                var result = await _userService.AssignManager(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // =====================================================
        // 🔹 5️⃣ UPDATE USER STATUS (Admin Only)
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequestDto request)
        {
            try
            {
                var result = await _userService.UpdateUserStatus(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // =====================================================
        // 🔹 6️⃣ UPDATE MY PROFILE (any authenticated user)
        // =====================================================
        [Authorize]
        [HttpPut("update-my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequestDto request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not identified." });
            try
            {
                var result = await _userService.UpdateMyProfile(userId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
