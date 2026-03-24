

using ReimbursementTrackerApp.Models.Common;
using ReimbursementTrackerApp.Models.DTOs;
using System.Threading.Tasks;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface IExpenseService
    {
        //Task<CreateExpenseResponseDto?> GetExpenseById(string expenseId);

        Task<PagedResponse<CreateExpenseResponseDto>> GetAllExpenses(
            PaginationParams paginationParams
        );

        Task<(bool IsSuccess, string Message, CreateExpenseResponseDto? Expense)>
            UpdateExpenseSafe(string expenseId, CreateExpenseRequestDto dto);

        Task<CreateExpenseResponseDto?> CreateExpense(CreateExpenseRequestDto request);

        Task<List<CreateExpenseResponseDto>> GetMyExpenses();



        Task<CreateExpenseResponseDto?> SubmitExpense(string expenseId);

        Task<(bool IsSuccess, string Message, CreateExpenseResponseDto? Expense)>
            DeleteExpenseSafe(string expenseId);
    }
}