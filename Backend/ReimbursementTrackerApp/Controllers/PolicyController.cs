
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;
        private readonly ILogger<PolicyController> _logger; // ✅ logger field

        public PolicyController(
            IPolicyService policyService,
            ILogger<PolicyController> logger) // ✅ inject logger
        {
            _policyService = policyService;
            _logger = logger;
        }

        // =====================================================
        // 🔹 Get All Policies (Employee/Admin/Finance)
        // =====================================================
        [HttpGet("getall")]
        [Authorize(Roles = "Employee,Manager,Finance,Admin")]
        public async Task<IActionResult> GetAllPolicies()
        {
            _logger.LogInformation("Request received to fetch all policies");

            try
            {
                var policies = await _policyService.GetAllPoliciesAsync();

                _logger.LogInformation("Successfully fetched {Count} policies", policies?.Count() ?? 0);

                return Ok(policies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching policies");

                return StatusCode(500, new
                {
                    message = "Failed to fetch policies.",
                    details = ex.Message
                });
            }
        }
    }
}