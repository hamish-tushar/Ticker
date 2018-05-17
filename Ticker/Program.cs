using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace Ticker
{
    class Program
    {
        public static Arguments arguments;
        private static IList<Error> errors;

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press Any Key to Quit");
                Console.ReadKey();
            }
        }

        static async Task MainAsync(string[] args)
        {
            var result = Parser.Default.ParseArguments<Arguments>(args);
            result.WithParsed(options => arguments = options);
            result.WithNotParsed(e => errors = e.ToList());

            if (errors?.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ToString());
                }
                return;
            }

            if (arguments.Debug)
            {
                Debugger.Launch();
            }
            
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

    public class Arguments
    {
        [Option('d', "debug", Required = false, HelpText = "Debug application")]
        public bool Debug { get; set; }

        [Option('s', "symbol", Required = false, HelpText = "Optional list of symbols override")]
        public string Symbol{ get; set; }

        [Option('r', "riskpercentage", Required = false, HelpText = "Risk percentage")]
        public int? RiskPercentage { get; set; }

        [Option('l', "lotsize", Required = false, HelpText = "Lot Size")]
        public int? LotSize { get; set; }

        [Option('p', "profit", Required = false, HelpText = "Profit threshold")]
        public int? Profit { get; set; }

        [Option('m', "margin", Required = false, HelpText = "Maximum margin")]
        public decimal? Margin { get; set; }
    }
}
