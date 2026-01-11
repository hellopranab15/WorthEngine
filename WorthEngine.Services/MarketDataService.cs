using System.Net.Http.Json;
using System.Text.Json;
using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services;

/// <summary>
/// Market Data Service for fetching real-time prices from external APIs.
/// - Mutual Funds: MFAPI.in (free, no auth required)
/// - Stocks: Yahoo Finance (via HTTP scraping for .NS symbols)
/// </summary>
public class MarketDataService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMutualFundRepository _mutualFundRepository;

    public MarketDataService(HttpClient httpClient, IMutualFundRepository mutualFundRepository)
    {
        _httpClient = httpClient;
        _mutualFundRepository = mutualFundRepository;
    }

    /// <summary>
    /// Fetches NAV from MFAPI.in for Indian Mutual Funds
    /// API: https://api.mfapi.in/mf/{SchemeCode}
    /// </summary>
    public async Task<decimal?> GetMutualFundNavAsync(string schemeCode)
    {
        var details = await GetMfDetailsAsync(schemeCode);
        return details?.CurrentNav;
    }

    public async Task<MfDetailsResponse?> GetMfDetailsAsync(string schemeCode)
    {
        if (string.IsNullOrEmpty(schemeCode))
            return null;

        try
        {
            // First check local DB
            var localScheme = await _mutualFundRepository.GetBySchemeCodeAsync(schemeCode);
            if (localScheme != null)
            {
                 // Return from local if we trust AMFI data freshness, 
                 // or fallback to API if local is old? 
                 // For now, let's prefer API for latest NAV, but local for name metadata.
                 // Actually the user wants to use lookup data to search. 
                 // If we have it locally, we should probably just return it unless we want real-time.
                 // The AMFI txt is usually updated daily.
                 
                 // If we want real-time, we still call API.
                 // Let's keep calling API for details view to ensure freshness, 
                 // but Search uses local DB.
            }

            var response = await _httpClient.GetAsync($"https://api.mfapi.in/mf/{schemeCode}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadFromJsonAsync<MfApiResponse>();
            
            if (json?.Data != null && json.Data.Count > 0)
            {
                var latestNav = json.Data[0];
                if (decimal.TryParse(latestNav.Nav, out decimal nav))
                {
                    // Basic parsing of date might be needed if format varies, usually dd-MM-yyyy
                    DateTime.TryParseExact(latestNav.Date, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime navDate);
                    
                    return new MfDetailsResponse(
                        json.Meta.SchemeCode.ToString(),
                        json.Meta.SchemeName,
                        nav,
                        navDate
                    );
                }
            }
        }
        catch (Exception)
        {
            // Log error
        }

        return null;
    }

    /// <summary>
    /// Fetches stock price from Yahoo Finance for Indian stocks (.NS suffix)
    /// Uses the chart API endpoint
    /// </summary>
    public async Task<decimal?> GetStockPriceAsync(string tickerSymbol)
    {
        if (string.IsNullOrEmpty(tickerSymbol))
            return null;

        try
        {
            // Yahoo Finance chart API
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{tickerSymbol}?interval=1d&range=1d";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            
            var result = json.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("regularMarketPrice")
                .GetDecimal();

            return result;
        }
        catch (Exception)
        {
            // Log error in production
        }

        return null;
    }

    /// <summary>
    /// Fetches stock metadata (company name, sector, market cap) from Yahoo Finance
    /// Uses the quote API endpoint
    /// </summary>
    public async Task<(string? companyName, string? sector, long? marketCap)> GetStockMetadataAsync(string tickerSymbol)
    {
        if (string.IsNullOrEmpty(tickerSymbol))
            return (null, null, null);

        try
        {
            // Yahoo Finance quote API
            var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={tickerSymbol}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return (null, null, null);

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            
            var quoteResponse = json.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result")[0];

            string? companyName = quoteResponse.TryGetProperty("longName", out var nameElement) 
                ? nameElement.GetString() 
                : quoteResponse.TryGetProperty("shortName", out var shortNameElement)
                    ? shortNameElement.GetString()
                    : null;

            string? sector = quoteResponse.TryGetProperty("sector", out var sectorElement) 
                ? sectorElement.GetString() 
                : null;

            long? marketCap = quoteResponse.TryGetProperty("marketCap", out var capElement) 
                ? capElement.GetInt64() 
                : null;

            return (companyName, sector, marketCap);
        }
        catch (Exception)
        {
            // Log error in production
        }

        return (null, null, null);
    }

    public async Task SyncAmfiDataAsync(string url)
    {
        try 
        {
            // Download the TXT file
            // Expected Format: Scheme Code;ISIN Div Payout/ISIN Growth;ISIN Div Reinvestment;Scheme Name;Net Asset Value;Date
            // 119551;INF209KA12Z1;INF209KA13Z9;Aditya Birla Sun Life Banking & PSU Debt Fund  - Direct Plan - Growth;100.1234;04-Apr-2024
            
            var response = await _httpClient.GetStringAsync(url);
            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            var schemes = new List<MutualFundScheme>();
            
            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length < 6) continue;
                
                // Skip header if typical header text
                if (parts[0].Contains("Scheme Code", StringComparison.OrdinalIgnoreCase)) continue;

                if (decimal.TryParse(parts[4], out decimal nav))
                {
                     DateTime.TryParse(parts[5], out DateTime date); // Try simplistic parse first

                     schemes.Add(new MutualFundScheme
                     {
                         SchemeCode = parts[0].Trim(),
                         IsinGrowth = parts[1].Trim(), // Assumption: 2nd col is Growth/Payout ISIN usually?
                         IsinDiv = parts[2].Trim(),
                         SchemeName = parts[3].Trim(),
                         NetAssetValue = nav,
                         Date = date
                     });
                }
            }

            if (schemes.Any())
            {
                await _mutualFundRepository.BulkInsertAsync(schemes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync Error: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<MutualFundScheme>> SearchSchemesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<MutualFundScheme>();
            
        return await _mutualFundRepository.SearchAsync(query);
    }
}

// MFAPI Response DTOs
internal class MfApiResponse
{
    public MfMeta Meta { get; set; } = new();
    public List<MfNavData> Data { get; set; } = new();
}

internal class MfMeta
{
    public string SchemeName { get; set; } = "";
    public int SchemeCode { get; set; }
}

internal class MfNavData
{
    public string Date { get; set; } = "";
    public string Nav { get; set; } = "";
}
