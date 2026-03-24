using System.Globalization;

namespace ReimbursementTrackerApp.Models.helper
{
    public static class CurrencyHelper
    {
        public static string FormatRupees(decimal amount)
        {
            return string.Format(new CultureInfo("en-IN"), "₹ {0:N2}", amount);
        }
    }
}
