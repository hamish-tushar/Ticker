using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ticker
{
    internal class Stocks
    {
        private string url = ConfigurationManager.AppSettings["Url"];
        public List<string> Symbols = (Program.arguments.Symbol ?? ConfigurationManager.AppSettings["Symbols"]).Split(',').ToList();
        private double RiskPercentage => (Program.arguments.RiskPercentage ?? double.Parse(ConfigurationManager.AppSettings["RiskPercentage"])) * .01;
        private int LotSize => (Program.arguments.LotSize ?? int.Parse(ConfigurationManager.AppSettings["LotSize"]));
        private int CallThreshold => Program.arguments.Profit ?? int.Parse(ConfigurationManager.AppSettings["CallThreshold"]);
        private int PutThreshold => Program.arguments.Profit ?? int.Parse(ConfigurationManager.AppSettings["PutThreshold"]);
        private int VolumeThreshold => int.Parse(ConfigurationManager.AppSettings["VolumeThreshold"]);
        private decimal MaxMargin => Program.arguments.Margin ?? decimal.Parse(ConfigurationManager.AppSettings["MaxMargin"]);

        private readonly HttpClient client;

        public Stocks()
        {
            client = new HttpClient();
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Margin {MaxMargin.ToCurrencyString()} Lots {LotSize} Risk percentage {(RiskPercentage * 100)}% Call profit ${CallThreshold} Put profit ${PutThreshold}");
            Console.WriteLine(string.Empty);
        }

        /// <summary>
        /// Even though it says third friday, yahoo API works with date expiry for Fri 12:00 am UTC
        /// Translates to Thursday 5pm PST
        /// </summary>
        /// <returns></returns>
        public long GetExpiryForThirdFriday(int futureMonth = 1)
        {
            var now = DateTime.UtcNow;
            DateTime thirdFriday = new DateTime(now.Year, now.Month, 15, 0, 0, 0, DateTimeKind.Utc).AddMonths(futureMonth);
            while (thirdFriday.DayOfWeek != DayOfWeek.Friday)
            {
                thirdFriday = thirdFriday.AddDays(1);
            }
            
            return new DateTimeOffset(thirdFriday).ToUnixTimeSeconds();
        }

        public async Task<string> GetStockPrice(string symbol, long expiry)
        {
            var fullUrl = string.Format(url, symbol, expiry);
            if (Program.arguments.Debug)
            {
                Console.WriteLine(fullUrl);
            }
            var response = await client.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var options = JsonConvert.DeserializeObject<Options>(responseString);

                if (options.optionChain.error != null)
                {
                    return null;
                }

                var result = options.optionChain.result[0];

                var calls = result.options[0].calls.OrderBy(c => c.strike).ToArray();
                for (int i = 0; i < calls.Length - 1; i ++)
                {
                    var sell = calls[i];
                    if (sell.inTheMoney)
                    {
                        continue;
                    }
                    var buy = FindBuyOption(calls, i);

                    if (IsCallOptionWorthIt(result.quote.regularMarketPrice, sell, buy, out var sellStrikePrice, out var buyStrikePrice, out var takeInMoney))
                    {
                        Display(result.underlyingSymbol, result.quote.regularMarketPrice, takeInMoney, sellStrikePrice, buyStrikePrice, sell.expiration, true);
                    }
                }


                var puts = result.options[0].puts.OrderBy(p => p.strike).ToArray();
                for (int i = puts.Length - 1; i > 0; i--)
                {
                    var sell = puts[i];
                    if (sell.inTheMoney)
                    {
                        continue;
                    }

                    var buy = FindBuyOption(puts, i);
                    
                    if (IsPutOptionWorthIt(result.quote.regularMarketPrice, buy, sell, out var sellStrikePrice, out var buyStrikePrice, out var takeInMoney))
                    {
                        Display(result.underlyingSymbol, result.quote.regularMarketPrice, takeInMoney, sellStrikePrice, buyStrikePrice, sell.expiration, false);
                    }
                }

            }
            return null;
        }

        private static void Display(string symbol, decimal regularMarketPrice, decimal takeInMoney, decimal sellStrikePrice, decimal buyStrikePrice, int expiration, bool isCall)
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expiration).DateTime;
            Console.WriteLine($"{symbol.PadRight(6)} {regularMarketPrice.ToString().PadRight(10)} profit {takeInMoney.ToCurrencyString().PadRight(7)} {(isCall ? "calls" : "puts")} {sellStrikePrice}/{buyStrikePrice} expiry {expiry.Subtract(DateTime.UtcNow).Days} days {expiry.ToShortDateString()}");
        }

        private Call FindBuyOption(Call[] calls, int i)
        {
            var current = calls[i];
            int callIndex = i + 1;
            for (int j = i + 1; j < calls.Length; j++)
            {
                var call = calls[j];
                var marginRequired = (call.strike - current.strike) * LotSize * 100;
                if (marginRequired > MaxMargin)
                {
                    break;
                }
                callIndex = j;
            }
            return calls[callIndex];
        }

        private Put FindBuyOption(Put[] puts, int i)
        {
            var current = puts[i];
            int putIndex = i -1;
            for (int j = i - 1; j > 0; j--)
            {
                var put = puts[j];
                var marginRequired = (current.strike - put.strike) * LotSize * 100;
                if (marginRequired > MaxMargin)
                {
                    break;
                }
                putIndex = j;
            }
            return puts[putIndex];
        }

        public bool IsCallOptionWorthIt(decimal currentPrice, Call sell, Call buy, out decimal sellStrikePrice, out decimal buyStrikePrice, out decimal takeInMoney)
        {
            sellStrikePrice = 0;
            buyStrikePrice = 0;
            takeInMoney = 0;
            if (sell.inTheMoney || buy.inTheMoney)
            {
                return false;
            }
            if (IsVolumeLow(sell, buy))
            {
                return false;
            }

            var marginRequired = Math.Abs(sell.strike - buy.strike) * LotSize * 100;
            if (marginRequired > MaxMargin)
            {
                return false;
            }

            if (sell.strike > currentPrice * (1 + (decimal)RiskPercentage))
            {
                var sellPrice = sell.bid;
                var buyPrice = buy.ask;
                var price = sellPrice - buyPrice;
                if (price * 100 * LotSize > CallThreshold)
                {
                    sellStrikePrice = sell.strike;
                    buyStrikePrice = buy.strike;
                    takeInMoney = price * 100 * LotSize;
                    return true;
                }
            }
            return false;
        }

        private bool IsVolumeLow(dynamic sell, dynamic buy)
        {
            return sell.openInterest < VolumeThreshold;
        }

        public bool IsPutOptionWorthIt(decimal currentPrice, Put buy, Put sell, out decimal sellStrikePrice, out decimal buyStrikePrice, out decimal takeInMoney)
        {
            sellStrikePrice = 0;
            buyStrikePrice = 0;
            takeInMoney = 0;
            if (sell.inTheMoney || buy.inTheMoney)
            {
                return false;
            }

            if (IsVolumeLow(sell, buy))
            {
                return false;
            }

            var marginRequired = Math.Abs(sell.strike - buy.strike) * LotSize * 100;
            if (marginRequired > MaxMargin)
            {
                return false;
            }

            if (sell.strike < currentPrice / (1 + (decimal)RiskPercentage))
            {
                var sellPrice = sell.bid;
                var buyPrice = buy.ask;
                var price = sellPrice - buyPrice;
                if (price * 100 * LotSize > PutThreshold)
                {
                    sellStrikePrice = sell.strike;
                    buyStrikePrice = buy.strike;
                    takeInMoney = price * 100 * LotSize;
                    return true;
                }
            }
            return false;
        }
    }
}
