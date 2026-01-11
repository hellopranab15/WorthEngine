namespace WorthEngine.Core.Helpers;

public static class MarketHelper
{
    public static readonly List<string> IndianDeepMovers = new()
    {
        "RELIANCE.NS", "TCS.NS", "HDFCBANK.NS", "INFY.NS", "HINDUNILVR.NS",
        "ICICIBANK.NS", "SBIN.NS", "BHARTIARTL.NS", "ITC.NS", "KOTAKBANK.NS",
        "LT.NS", "AXISBANK.NS", "ASIANPAINT.NS", "HCLTECH.NS", "MARUTI.NS",
        "TITAN.NS", "BAJFINANCE.NS", "SUNPHARMA.NS", "TATAMOTORS.NS", "ULTRACEMCO.NS",
        "POWERGRID.NS", "NTPC.NS", "M&M.NS", "TATASTEEL.NS", "JSWSTEEL.NS",
        "ADANIENT.NS", "ADANIPORTS.NS", "COALINDIA.NS", "ONGC.NS", "BPCL.NS"
    };

    public static readonly List<string> USDeepMovers = new()
    {
        "AAPL", "MSFT", "GOOG", "AMZN", "NVDA", "TSLA", "META", "BRK-B", "TSM", "UNH",
        "JNJ", "XOM", "V", "JPM", "WMT", "PG", "MA", "LLY", "CVX", "HD",
        "ABBV", "MRK", "KO", "PEP", "AVGO", "COST", "ORCL", "MCD", "CSCO", "CRM"
    };
}
