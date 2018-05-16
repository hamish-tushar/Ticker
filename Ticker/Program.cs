using System;
using System.Threading.Tasks;

namespace Ticker
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
            Console.WriteLine("Press Any Key to Quit");
            Console.ReadKey();
        }

        static async Task MainAsync(string[] args)
        {
            var stocks = new Stocks();
            var time = stocks.GetExpiryForThirdFriday(DateTime.Now, 2);
            foreach (var symbol in stocks.Symbols)
            {
                await stocks.GetStockPrice(symbol, time);
            }

            time = stocks.GetExpiryForThirdFriday(DateTime.Now, 1);

            foreach (var symbol in stocks.Symbols)
            {
                await stocks.GetStockPrice(symbol, time);
            }
        }
    }
}
