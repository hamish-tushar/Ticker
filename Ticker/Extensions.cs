namespace Ticker
{
    public static class Extensions
    {
        public static string ToCurrencyString(this decimal d, bool noPrecision = true)
        {
            return d.ToString(noPrecision ? "c0" : "c");
        }

        public static decimal ToDecimalTotal(this int d)
        {
            return d / (decimal)100.0;
        }
        
    }
}