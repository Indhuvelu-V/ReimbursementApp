using ReimbursementTrackerApp.Contexts;
using ReimbursementTrackerApp.Models;
using ReimbursementTrackerApp.Models.Enums;

namespace ReimbursementTrackerApp.Data
{
    public static class CategorySeeder
    {
        public static async Task SeedCategoriesAsync(ReimbursementContext context)
        {
            if (context.ExpenseCategories.Any())
                return; // Already seeded

            var categories = new List<ExpenseCategory>
            {
                new ExpenseCategory
                {
                    CategoryId = Guid.NewGuid().ToString(),
                    CategoryName = ExpenseCategoryType.Travel,
                    MaxLimit = 5000
                },
                new ExpenseCategory
                {
                    CategoryId = Guid.NewGuid().ToString(),
                    CategoryName = ExpenseCategoryType.Food,
                    MaxLimit = 1000
                },
                new ExpenseCategory
                {
                    CategoryId = Guid.NewGuid().ToString(),
                    CategoryName = ExpenseCategoryType.Medical,
                    MaxLimit = 10000
                },
                new ExpenseCategory
                {
                    CategoryId = Guid.NewGuid().ToString(),
                    CategoryName = ExpenseCategoryType.OfficeSupplies,
                    MaxLimit = 3000
                }
            };

            context.ExpenseCategories.AddRange(categories);
            await context.SaveChangesAsync();
        }
    }
}