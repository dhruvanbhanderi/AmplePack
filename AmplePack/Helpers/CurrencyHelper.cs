using System.Globalization;

namespace AmplePack.Helpers
{
    /// <summary>
    /// Helper class for consistent currency formatting throughout the application
    /// </summary>
    public static class CurrencyHelper
    {
        // Indian Rupee prefix
        private const string CURRENCY_SYMBOL = "Rs.";
        
        /// <summary>
        /// Formats a decimal amount as currency with Indian Rupee prefix
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted currency string (e.g., "Rs.1,234.56")</returns>
        public static string FormatCurrency(decimal amount)
        {
            return $"{CURRENCY_SYMBOL}{amount:N2}";
        }
        
        /// <summary>
        /// Formats a decimal amount as currency without comma separators
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted currency string (e.g., "Rs.1234.56")</returns>
        public static string FormatCurrencySimple(decimal amount)
        {
            return $"{CURRENCY_SYMBOL}{amount:F2}";
        }
        
        /// <summary>
        /// Gets just the currency symbol
        /// </summary>
        /// <returns>The currency symbol (Rs.)</returns>
        public static string GetCurrencySymbol()
        {
            return CURRENCY_SYMBOL;
        }
        
        /// <summary>
        /// Formats amount for input fields (without symbol)
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted amount for input (e.g., "1234.56")</returns>
        public static string FormatForInput(decimal amount)
        {
            return amount.ToString("F2", CultureInfo.InvariantCulture);
        }
        
        /// <summary>
        /// Parses a currency string back to decimal
        /// </summary>
        /// <param name="currencyString">String with or without currency symbol</param>
        /// <returns>Parsed decimal amount</returns>
        public static decimal ParseCurrency(string currencyString)
        {
            if (string.IsNullOrWhiteSpace(currencyString))
                return 0;
            
            // Remove currency symbol and any whitespace
            var cleanString = currencyString.Replace(CURRENCY_SYMBOL, "").Trim();
            
            if (decimal.TryParse(cleanString, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal result))
                return result;
            
            return 0;
        }
        
        /// <summary>
        /// Validates if a decimal amount is valid for currency
        /// </summary>
        /// <param name="amount">Amount to validate</param>
        /// <returns>True if valid currency amount</returns>
        public static bool IsValidCurrencyAmount(decimal amount)
        {
            return amount >= 0 && amount <= decimal.MaxValue;
        }
        
        /// <summary>
        /// Rounds amount to 2 decimal places for currency precision
        /// </summary>
        /// <param name="amount">Amount to round</param>
        /// <returns>Rounded amount</returns>
        public static decimal RoundCurrency(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}