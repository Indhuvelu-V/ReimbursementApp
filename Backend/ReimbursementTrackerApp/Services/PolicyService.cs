
using Microsoft.Extensions.Logging;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PolicyService : IPolicyService
{
    private readonly IRepository<string, Policy> _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PolicyService> _logger; // Added ILogger

    public PolicyService(
        IRepository<string, Policy> repository,
        IAuditLogService auditLogService,
        ILogger<PolicyService> logger) // Inject ILogger
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    // =====================================================
    // 🔹 Get All Policies
    // =====================================================
    public async Task<IEnumerable<CreatePolicyResponseDto>> GetAllPoliciesAsync()
    {
        _logger.LogInformation("Fetching all policies.");

        try
        {
            var policies = await _repository.GetAllAsync();
            var policyCount = policies?.Count() ?? 0;

            _logger.LogInformation("Fetched {Count} policies.", policyCount);

            // Audit log for fetching all policies
            await _auditLogService.CreateLog(new CreateAuditLogsRequestDto
            {
                Action = "Fetched all policies",
                Description = $"Total policies fetched: {policyCount}"
            });

            return policies?.Select(p => new CreatePolicyResponseDto
            {
                PolicyId = p.PolicyId,
                CategoryId = p.CategoryId,
                Description = p.Description,
                CategoryName = p.CategoryName
            }).ToList() ?? new List<CreatePolicyResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching policies.");
            throw new Exception("Unexpected error while fetching policies.", ex);
        }
    }
}