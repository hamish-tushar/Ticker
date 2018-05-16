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
        public List<string> Symbols = ConfigurationManager.AppSettings["Symbols"].Split(',').ToList();
        private double RiskValue => 1 + double.Parse(ConfigurationManager.AppSettings["RiskPercentage"]) * .01;
        private int LotSize => int.Parse(ConfigurationManager.AppSettings["LotSize"]);
        private int CallThreshold => int.Parse(ConfigurationManager.AppSettings["CallThreshold"]);
        private int PutThreshold => int.Parse(ConfigurationManager.AppSettings["PutThreshold"]);
        private readonly HttpClient client;

        public Stocks()
        {
            client = new HttpClient();
        }

        /// <summary>
        /// Even though it says third friday, yahoo API works with date expiry for Fri 12:00 am UTC
        /// Translates to Thursday 5pm PST
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="futureMonth"></param>
        /// <returns></returns>
        public long GetExpiryForThirdFriday(DateTime dateTime, int futureMonth = 1)
        {
            var now = DateTime.Now;
            var expiryYear = now.Year;
            switch (futureMonth)
            {
                case 1:
                    if (now.Month == 11)
                    {
                        expiryYear = now.Year + 1;
                    }
                    break;
                case 2:
                    if (now.Month == 10 || now.Year == 11)
                    {
                        expiryYear = now.Year + 1;
                    }
                    break;
            }
            
            var thirdFriday = new DateTime(expiryYear, (now.Month + futureMonth) % 12, 1, 17, 0, 0);

            while (thirdFriday.DayOfWeek != DayOfWeek.Thursday)
            {
                thirdFriday = thirdFriday.AddDays(1);
            }

            thirdFriday = thirdFriday.AddDays(14);

            return new DateTimeOffset(thirdFriday).ToUnixTimeSeconds();
        }

        public async Task<string> GetStockPrice(string symbol, long expiry)
        {
            var response = await client.GetAsync(string.Format(url, symbol, expiry));
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var options = JsonConvert.DeserializeObject<Options>(responseString);

                if (options.optionChain.error == null)
                    for (int i =0; i < options.optionChain.result[0].options[0].calls.Length - 1; i+=2)
                    {
                        var sell = options.optionChain.result[0].options[0].calls[i];
                        var buy = options.optionChain.result[0].options[0].calls[i + 1];
                        if (IsCallOptionWorthIt(options.optionChain.result[0].quote.regularMarketPrice, sell, buy,
                            out var sellStrikePrice, out var buytrikePrice, out var takeInMoney))
                        {
                            Console.WriteLine($"Stock {options.optionChain.result[0].underlyingSymbol} {options.optionChain.result[0].quote.regularMarketPrice} has potential for ${takeInMoney} strike Price Calls {sellStrikePrice}/{buytrikePrice} and Expiry {DateTimeOffset.FromUnixTimeSeconds(sell.expiration).LocalDateTime} ");
                        }
                    }

                for (int i = 0; i < options.optionChain.result[0].options[0].puts.Length - 1; i += 2)
                {
                    var buy = options.optionChain.result[0].options[0].puts[i];
                    var sell = options.optionChain.result[0].options[0].puts[i + 1];
                    if (IsPutOptionWorthIt(options.optionChain.result[0].quote.regularMarketPrice, buy, sell,
                        out var sellStrikePrice, out var buytrikePrice, out var takeInMoney))
                    {
                        Console.WriteLine($"Stock {options.optionChain.result[0].underlyingSymbol} {options.optionChain.result[0].quote.regularMarketPrice} has potential for ${takeInMoney} strike Price Puts {buytrikePrice}/{sellStrikePrice} and Expiry {DateTimeOffset.FromUnixTimeSeconds(sell.expiration).LocalDateTime} ");
                    }
                }

            }
            return null;
        }


        public bool IsCallOptionWorthIt(double currentPrice, Call sell, Call buy, out float sellStrikePrice, out float buyStrikePrice, out double takeInMoney)
        {
            sellStrikePrice = 0;
            buyStrikePrice = 0;
            takeInMoney = 0;
            if (sell.inTheMoney || buy.inTheMoney)
            {
                return false;
            }

            if (sell.strike > currentPrice * RiskValue)
            {
                var midSell = (sell.bid + sell.ask) / 2;
                var midBuy = (buy.bid + buy.ask) / 2;
                if ((midSell - midBuy) * 100 * LotSize > CallThreshold)
                {
                    sellStrikePrice = sell.strike;
                    buyStrikePrice = buy.strike;
                    takeInMoney = (midSell - midBuy) *100 * LotSize;
                    return true;
                }
            }
            return false;
        }


        public bool IsPutOptionWorthIt(double currentPrice, Put buy, Put sell, out float sellStrikePrice, out float buyStrikePrice, out double takeInMoney)
        {
            sellStrikePrice = 0;
            buyStrikePrice = 0;
            takeInMoney = 0;
            if (sell.inTheMoney || buy.inTheMoney)
            {
                return false;
            }

            if (sell.strike < currentPrice / RiskValue)
            {
                var midSell = (sell.bid + sell.ask) / 2;
                var midBuy = (buy.bid + buy.ask) / 2;
                if ((midSell - midBuy) * 100 * LotSize > PutThreshold)
                {
                    sellStrikePrice = sell.strike;
                    buyStrikePrice = buy.strike;
                    takeInMoney = (midSell - midBuy) * 100 * LotSize;
                    return true;
                }
            }
            return false;
        }
    }
}
