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
        private double RiskValue => 1 + (Program.arguments.RiskPercentage ?? double.Parse(ConfigurationManager.AppSettings["RiskPercentage"])) * .01;
        private int LotSize => (Program.arguments.LotSize ?? int.Parse(ConfigurationManager.AppSettings["LotSize"]));
        private int CallThreshold => Program.arguments.Profit ?? int.Parse(ConfigurationManager.AppSettings["CallThreshold"]);
        private int PutThreshold => Program.arguments.Profit ?? int.Parse(ConfigurationManager.AppSettings["PutThreshold"]);
        private int VolumeThreshold => int.Parse(ConfigurationManager.AppSettings["VolumeThreshold"]);
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

                if (options.optionChain.error != null)
                {
                    return null;
                }

                var result = options.optionChain.result[0];

                var calls = result.options[0].calls;
                for (int i = 0; i < calls.Length - 1; i += 2)
                {
                    var sell = calls[i];
                    var buy = calls[i + 1];

                    if (IsCallOptionWorthIt(result.quote.regularMarketPrice, sell, buy, out var sellStrikePrice, out var buyStrikePrice, out var takeInMoney))
                    {
                        Console.WriteLine(
                            $"{result.underlyingSymbol} {result.quote.regularMarketPrice} has potential for {takeInMoney.ToCurrencyString()} calls {sellStrikePrice}/{buyStrikePrice} and Expiry {DateTimeOffset.FromUnixTimeSeconds(sell.expiration).LocalDateTime.ToShortDateString()} ");
                    }
                }


                var puts = result.options[0].puts;
                for (int i = 0; i < puts.Length - 1; i += 2)
                {
                    var buy = puts[i];
                    var sell = puts[i + 1];
                    if (IsPutOptionWorthIt(result.quote.regularMarketPrice, buy, sell, out var sellStrikePrice, out var buyStrikePrice, out var takeInMoney))
                    {
                        Console.WriteLine(
                            $"{result.underlyingSymbol} {result.quote.regularMarketPrice} has potential for {takeInMoney.ToCurrencyString()} puts {sellStrikePrice}/{buyStrikePrice} and Expiry {DateTimeOffset.FromUnixTimeSeconds(sell.expiration).LocalDateTime.ToShortDateString()} ");
                    }
                }

            }
            return null;
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
            if (sell.strike > currentPrice * (decimal)RiskValue)
            {
                var sellPrice = sell.bid;
                var buyPrice = buy.ask;
                var price = (sellPrice - buyPrice) / 2;
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
            return sell.volume < VolumeThreshold || buy.volume < VolumeThreshold;
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

            if (sell.strike < currentPrice / (decimal)RiskValue)
            {
                var sellPrice = sell.bid;
                var buyPrice = buy.ask;
                var price = (sellPrice - buyPrice) / 2;
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
