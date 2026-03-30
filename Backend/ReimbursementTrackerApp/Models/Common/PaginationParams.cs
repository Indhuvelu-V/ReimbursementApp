namespace ReimbursementTrackerApp.Models.Common
{
    public class PaginationParams
    {
        private const int MaxPageSize = 200;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public string? Role { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? UserName { get; set; }
        public string? Action { get; set; }
    }
}