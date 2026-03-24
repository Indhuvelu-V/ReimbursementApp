
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ✅ add this
using ReimbursementTrackerApp.Exceptions;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger; // ✅ logger

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger) // ✅ inject
        {
            _authService = authService;
            _logger = logger;
        }

        // =====================================================
        // LOGIN
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CheckUserRequestDto request)
        {
            _logger.LogInformation("Login attempt for User {UserName}", request.UserName);

            try
            {
                var result = await _authService.CheckUser(request);

                _logger.LogInformation("Login successful for User {UserName}", request.UserName);

                return Ok(result);
            }
            catch (UnAuthorizedException ex)
            {
                _logger.LogWarning(ex, "Invalid login attempt for User {UserName}", request.UserName);

                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for User {UserName}", request.UserName);

                return StatusCode(500, new
                {
                    message = "Unexpected error during login",
                    details = ex.Message
                });
            }
        }
    }
}