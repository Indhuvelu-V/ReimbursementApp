namespace ReimbursementTrackerApp.Models.Common
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // Add these two for date filtering
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }
}