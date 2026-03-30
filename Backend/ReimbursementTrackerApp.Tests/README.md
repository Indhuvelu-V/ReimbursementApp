# ReimbursementTrackerApp — xUnit Tests

## Test Coverage

| Service | Tests |
|---|---|
| `AuthService` | Login valid/invalid credentials, user not found, case-sensitive username |
| `UserService` | Create user, duplicate user, get by ID, filter by role/name, pagination |
| `ExpenseService` | Create expense, amount limit, duplicate, submit, delete, filter by status |
| `ApprovalService` | Approve, reject, self-approval block, not found, not submitted, get all paged |
| `PaymentService` | Complete payment, not found, not approved, get all with status/amount filters |
| `AuditLogService` | Create log, get paged, date filter, pagination, delete (admin only) |
| `NotificationService` | Create, get by user, mark as read, reply (manager/system) |

## Running Tests

### From Package Manager Console (Visual Studio)
```
Test-Project ReimbursementTrackerApp.Tests
```

### From terminal (in Backend folder)
```bash
dotnet test ReimbursementTrackerApp.Tests/ReimbursementTrackerApp.Tests.csproj
```

### Run with detailed output
```bash
dotnet test --verbosity normal
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~ExpenseServiceTests"
```

## Stack
- **xUnit** — test framework
- **Moq** — mocking dependencies
- **FluentAssertions** — readable assertions
