using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ReimbursementTrackerApp.Filters;
using ReimbursementTrackerApp.Interfaces;
using ReimbursementTrackerApp.Models.DTOs;
using Xunit;

namespace ReimbursementTrackerApp.Tests.Services
{
    public class AuditLogActionFilterTests
    {
        private readonly Mock<IAuditLogService>              _auditService = new();
        private readonly Mock<ILogger<AuditLogActionFilter>> _logger       = new();

        private AuditLogActionFilter CreateFilter() =>
            new(_auditService.Object, _logger.Object);

        private void SetupAudit() =>
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ReturnsAsync(new CreateAuditLogsResponseDto());

        private ActionExecutingContext MakeContext(
            string userId, string role, string controller, string action,
            string httpMethod = "POST", string? expenseId = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, role)
            };
            var httpCtx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };
            httpCtx.Request.Method = httpMethod;

            var routeData = new RouteData();
            routeData.Values["controller"] = controller;
            routeData.Values["action"]     = action;

            var actionDescriptor = new ActionDescriptor
            {
                RouteValues = new Dictionary<string, string?>
                {
                    ["controller"] = controller,
                    ["action"]     = action
                }
            };

            var actionContext = new ActionContext(httpCtx, routeData, actionDescriptor);
            var args = new Dictionary<string, object?>();
            if (expenseId != null) args["expenseId"] = expenseId;

            return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), args, new object());
        }

        private ActionExecutionDelegate MakeDelegate() =>
            () => Task.FromResult(new ActionExecutedContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new object()));

        // ── Unauthenticated ───────────────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_NoUserId_SkipsAuditLog()
        {
            var httpCtx = new DefaultHttpContext { User = new ClaimsPrincipal() };
            httpCtx.Request.Method = "POST";
            var actionDescriptor = new ActionDescriptor
            {
                RouteValues = new Dictionary<string, string?> { ["controller"] = "Expense", ["action"] = "CreateExpense" }
            };
            var ctx = new ActionExecutingContext(
                new ActionContext(httpCtx, new RouteData(), actionDescriptor),
                new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());

            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            _auditService.Verify(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()), Times.Never);
        }

        // ── Submit ────────────────────────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_SubmitAction_LogsSubmitted()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var ctx = MakeContext("U1", "Employee", "Expense", "SubmitExpense", "POST", "E1");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured!.Action.Should().Contain("Submitted");
        }

        // ── GET skipped ───────────────────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_GetRequest_SkipsAuditLog()
        {
            var ctx = MakeContext("U1", "Admin", "Expense", "GetAllExpenses", "GET");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            _auditService.Verify(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()), Times.Never);
        }

        // ── CompletePayment skipped ───────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_CompletePayment_SkipsAuditLog()
        {
            var ctx = MakeContext("U1", "Finance", "Payment", "CompletePayment", "POST", "E1");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            _auditService.Verify(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()), Times.Never);
        }

        // ── PUT ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_PutRequest_LogsUpdated()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            // Use a PUT action that is NOT in serviceHandled — e.g. UpdateCategoryLimit
            var ctx = MakeContext("U1", "Admin", "ExpenseCategory", "UpdateCategoryLimit", "PUT");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured!.Action.Should().Contain("Updated");
        }

        // ── Manager Approval ──────────────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_ManagerApproval_SkipsLog()
        {
            // ManagerApproval is in serviceHandled — ApprovalService logs it directly
            var ctx = MakeContext("MGR1", "Manager", "Approval", "ManagerApproval", "POST");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            _auditService.Verify(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()), Times.Never);
        }

        // ── AuditLog failure swallowed ────────────────────────────────────────

        [Fact]
        public async Task OnActionExecutionAsync_AuditLogThrows_DoesNotPropagateException()
        {
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .ThrowsAsync(new Exception("Audit failure"));

            var ctx = MakeContext("U1", "Employee", "Expense", "SubmitExpense", "POST", "E1");

            var ex = await Record.ExceptionAsync(() =>
                CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate()));

            ex.Should().BeNull();
        }

        [Fact]
        public async Task OnActionExecutionAsync_ApproveAction_LogsApproval()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var ctx = MakeContext("U1", "Manager", "Approval", "ApproveExpense", "POST");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured!.Action.Should().Contain("Approval");
        }

        [Fact]
        public async Task OnActionExecutionAsync_PostWithNoExpenseId_LogsWithEmptyId()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            // Use a POST action NOT in serviceHandled — e.g. CreateNotification
            var ctx = MakeContext("U1", "Manager", "Notification", "CreateNotification", "POST");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured.Should().NotBeNull();
            captured!.Action.Should().Contain("Created");
        }

        [Fact]
        public async Task OnActionExecutionAsync_UnknownAction_LogsFallback()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            // PATCH is not GET/POST/PUT/DELETE — hits else branch
            var ctx = MakeContext("U1", "Admin", "Expense", "SomeCustomAction", "PATCH");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured.Should().NotBeNull();
            captured!.Action.Should().Contain("SomeCustomAction");
        }

        [Fact]
        public async Task OnActionExecutionAsync_AdminRole_ParsedCorrectly()
        {
            SetupAudit();
            CreateAuditLogsRequestDto? captured = null;
            _auditService.Setup(a => a.CreateLog(It.IsAny<CreateAuditLogsRequestDto>()))
                .Callback<CreateAuditLogsRequestDto>(r => captured = r)
                .ReturnsAsync(new CreateAuditLogsResponseDto());

            var ctx = MakeContext("U1", "Admin", "AuditLogs", "DeleteLog", "DELETE");
            await CreateFilter().OnActionExecutionAsync(ctx, MakeDelegate());

            captured!.Role.Should().Be(UserRole.Admin);
        }

    }
}
