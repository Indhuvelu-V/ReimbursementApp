
using Microsoft.EntityFrameworkCore;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Contexts
{
    public class ReimbursementContext : DbContext
    {
        public ReimbursementContext(DbContextOptions<ReimbursementContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;
        public DbSet<Policy> Policies { get; set; } = null!;
        public DbSet<Approval> Approvals { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // ENUM STORAGE (STORE AS INT IN DB)
            // =====================================================

            modelBuilder.Entity<Expense>()
                .Property(e => e.Status)
                .HasConversion<int>();

            modelBuilder.Entity<ExpenseCategory>()
                .Property(c => c.CategoryName)
                .HasConversion<int>();

            modelBuilder.Entity<Approval>()
                .Property(a => a.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentStatus)
                .HasConversion<int>();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            modelBuilder.Entity<User>()
                .Property(u => u.Department)
                .HasConversion<int?>()
                .IsRequired(false);

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<int>();

            modelBuilder.Entity<User>()
                .Property(u => u.ApprovalLevel)
                .HasConversion<int>();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Role)
                .HasConversion<int>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.SenderRole)
                .HasDefaultValue("Manager");

            // =====================================================
            // PRIMARY KEYS
            // =====================================================

            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Expense>().HasKey(e => e.ExpenseId);
            modelBuilder.Entity<ExpenseCategory>().HasKey(c => c.CategoryId);
            modelBuilder.Entity<Policy>().HasKey(p => p.PolicyId);
            modelBuilder.Entity<Approval>().HasKey(a => a.ApprovalId);
            modelBuilder.Entity<Payment>().HasKey(p => p.PaymentId);
            modelBuilder.Entity<Notification>().HasKey(n => n.NotificationId);
            modelBuilder.Entity<AuditLog>().HasKey(a => a.LogId);



            // =====================================================
            // RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<User>()
                .HasMany(u => u.Expenses)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict); // don't delete user when expense deleted

            modelBuilder.Entity<User>()
                .HasMany(u => u.Approvals)
                .WithOne(a => a.Manager)
                .HasForeignKey(a => a.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // safe: delete notifications if user deleted

            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing: User has a reporting Manager (same entity)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithMany()
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<Expense>()
                .HasMany(e => e.Approvals)
                .WithOne(a => a.Expense)
                .HasForeignKey(a => a.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict); // prevent cascade deletion

            modelBuilder.Entity<Expense>()
                .HasMany(e => e.Payments)
                .WithOne(p => p.Expense)
                .HasForeignKey(p => p.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict); // prevent cascade deletion

            modelBuilder.Entity<ExpenseCategory>()
                .HasMany(c => c.Expenses)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseCategory>()
                .HasMany(c => c.Policies)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // DECIMAL PRECISION (FINANCIAL VALUES)
            // =====================================================

            modelBuilder.Entity<ExpenseCategory>()
                .Property(c => c.MaxLimit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(e => e.OldAmount).HasPrecision(18, 2);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // =====================================================
            // SEED DATA (ONLY CATEGORY + POLICY)
            // =====================================================

            modelBuilder.Entity<ExpenseCategory>().HasData(
                new ExpenseCategory
                {
                    CategoryId = "C1",
                    CategoryName = ExpenseCategoryType.Travel,
                    MaxLimit = 5000
                },
                new ExpenseCategory
                {
                    CategoryId = "C2",
                    CategoryName = ExpenseCategoryType.Food,
                    MaxLimit = 1000
                },
                new ExpenseCategory
                {
                    CategoryId = "C3",
                    CategoryName = ExpenseCategoryType.Medical,
                    MaxLimit = 10000
                },
                new ExpenseCategory
                {
                    CategoryId = "C4",
                    CategoryName = ExpenseCategoryType.OfficeSupplies,
                    MaxLimit = 3000
                }
            );

            modelBuilder.Entity<Policy>().HasData(
                new Policy
                {
                    PolicyId = "P1",
                    CategoryId = "C1",
                    CategoryName = "Travel",
                    Description = "Travel reimbursement policy"
                },
                new Policy
                {
                    PolicyId = "P2",
                    CategoryId = "C2",
                    CategoryName = "Food",
                    Description = "Food reimbursement policy"
                },
                new Policy
                {
                    PolicyId = "P3",
                    CategoryId = "C3",
                    CategoryName = "Medical",
                    Description = "Medical reimbursement policy"
                },
                new Policy
                {
                    PolicyId = "P4",
                    CategoryId = "C4",
                    CategoryName = "OfficeSupplies",
                    Description = "Office supplies reimbursement policy"
                }
            );
        }
    }
}
