namespace Ticker
{
    public static class Extensions
    {
        public static string ToCurrencyString(this decimal d)
        {
            return d.ToString("c");
        }

        public static decimal ToDecimalTotal(this int d)
        {
            return d / (decimal)100.0;
        }
        
    }
}