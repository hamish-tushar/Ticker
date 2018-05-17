using Newtonsoft.Json;

namespace Ticker
{

    class Options
    {
        public Optionchain optionChain { get; set; }
    }

    public class Optionchain
    {
        public Result[] result { get; set; }
        public object error { get; set; }
    }

    public class Result
    {
        public string underlyingSymbol { get; set; }
        public int[] expirationDates { get; set; }
        public decimal[] strikes { get; set; }
        public bool hasMiniOptions { get; set; }
        public Quote quote { get; set; }
        public Option[] options { get; set; }
    }

    public class Quote
    {
        public string language { get; set; }
        public string quoteType { get; set; }
        public string quoteSourceName { get; set; }
        public string currency { get; set; }
        public decimal twoHundredDayAverageChange { get; set; }
        public decimal twoHundredDayAverageChangePercent { get; set; }
        public long marketCap { get; set; }
        public int sourceInterval { get; set; }
        public string exchangeTimezoneName { get; set; }
        public string exchangeTimezoneShortName { get; set; }
        public int gmtOffSetMilliseconds { get; set; }
        public bool esgPopulated { get; set; }
        public bool tradeable { get; set; }
        public long sharesOutstanding { get; set; }
        public decimal fiftyDayAverage { get; set; }
        public decimal fiftyDayAverageChange { get; set; }
        public decimal fiftyDayAverageChangePercent { get; set; }
        public decimal twoHundredDayAverage { get; set; }
        public string marketState { get; set; }
        public decimal regularMarketPrice { get; set; }
        public int regularMarketTime { get; set; }
        public decimal regularMarketChange { get; set; }
        public decimal regularMarketOpen { get; set; }
        public decimal regularMarketDayHigh { get; set; }
        public decimal regularMarketDayLow { get; set; }
        public int regularMarketVolume { get; set; }
        public string fullExchangeName { get; set; }
        public string longName { get; set; }
        public string financialCurrency { get; set; }
        public int averageDailyVolume3Month { get; set; }
        public int averageDailyVolume10Day { get; set; }
        public decimal fiftyTwoWeekLowChange { get; set; }
        public decimal fiftyTwoWeekLowChangePercent { get; set; }
        public string fiftyTwoWeekRange { get; set; }
        public decimal fiftyTwoWeekHighChange { get; set; }
        public decimal fiftyTwoWeekHighChangePercent { get; set; }
        public decimal fiftyTwoWeekLow { get; set; }
        public decimal fiftyTwoWeekHigh { get; set; }
        public decimal ytdReturn { get; set; }
        public decimal trailingThreeMonthReturns { get; set; }
        public decimal trailingThreeMonthNavReturns { get; set; }
        public int exchangeDataDelayedBy { get; set; }
        public string shortName { get; set; }
        public decimal postMarketChangePercent { get; set; }
        public int postMarketTime { get; set; }
        public decimal postMarketPrice { get; set; }
        public decimal postMarketChange { get; set; }
        public decimal regularMarketChangePercent { get; set; }
        public string regularMarketDayRange { get; set; }
        public decimal regularMarketPreviousClose { get; set; }
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public int bidSize { get; set; }
        public int askSize { get; set; }
        public string messageBoardId { get; set; }
        public int priceHint { get; set; }
        public string market { get; set; }
        public string exchange { get; set; }
        public string symbol { get; set; }
    }

    public class Option
    {
        public int expirationDate { get; set; }
        public bool hasMiniOptions { get; set; }
        public Call[] calls { get; set; }
        public Put[] puts { get; set; }
    }

    public class Call
    {
        public string contractSymbol { get; set; }
        public decimal strike { get; set; }
        public string currency { get; set; }
        public decimal lastPrice { get; set; }
        public decimal change { get; set; }
        public decimal percentChange { get; set; }
        public int volume { get; set; }
        public int openInterest { get; set; }
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public string contractSize { get; set; }
        public int expiration { get; set; }
        public int lastTradeDate { get; set; }
        public decimal impliedVolatility { get; set; }
        public bool inTheMoney { get; set; }
    }

    public class Put
    {
        public string contractSymbol { get; set; }
        public decimal strike { get; set; }
        public string currency { get; set; }
        public decimal lastPrice { get; set; }
        public decimal change { get; set; }
        public decimal percentChange { get; set; }
        public int volume { get; set; }
        public int openInterest { get; set; }
        public decimal bid { get; set; }
        public decimal ask { get; set; }
        public string contractSize { get; set; }
        public int expiration { get; set; }
        public int lastTradeDate { get; set; }
        public decimal impliedVolatility { get; set; }
        public bool inTheMoney { get; set; }
    }




}
