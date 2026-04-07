
namespace ReimbursementTrackerApp.Models.Enums
{
    public enum ExpenseStatus
    {
        Draft,
        Submitted,
        Approved,
        Pending,
        Rejected,
        Paid
    }
    public enum UserRole
    {
        Admin,
        Employee,
        Manager,
        Finance
    }

    public enum DepartmentType
    {
        IT,
        HR,
        FinanceAnalyst,
        Sales
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
    public enum PaymentStatusEnum
    {
        Pending,
        Paid,
        Approved
    }

    public enum ExpenseCategoryType
    {
        Travel,
        Food,
        Medical,
        OfficeSupplies
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Suspended
    }

    public enum ApprovalLevel
    {
        Level1,
        Level2,
        Finance
    }


}
