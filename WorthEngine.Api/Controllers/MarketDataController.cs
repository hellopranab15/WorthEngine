using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;
using WorthEngine.Core.Helpers;

namespace WorthEngine.Api.Controllers;

[ApiController]
[Route("api/market")]
[Authorize]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketDataController(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("mf/{schemeCode}")]
    public async Task<ActionResult<MfDetailsResponse>> GetMfDetails(string schemeCode)
    {
        var details = await _marketDataService.GetMfDetailsAsync(schemeCode);
        if (details == null)
            return NotFound("Scheme not found or invalid.");
        
        return Ok(details);
    }

    [HttpPost("sync-amfi")]
    public async Task<IActionResult> SyncAmfiData([FromBody] SyncRequest request)
    {
        try 
        {
            await _marketDataService.SyncAmfiDataAsync(request.Url);
            return Ok(new { message = "Sync started successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<MutualFundScheme>>> SearchSchemes([FromQuery] string q)
    {
        var results = await _marketDataService.SearchSchemesAsync(q);
        return Ok(results);
    }

    [HttpGet("search-stocks")]
    public async Task<ActionResult<List<StockSearchResult>>> SearchStocks([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Ok(new List<StockSearchResult>());
        var results = await _marketDataService.SearchStocksAsync(query);
        return Ok(results);
    }

    [HttpGet("movers")]
    public async Task<ActionResult<MarketMoversResponse>> GetMovers([FromQuery] string market = "IN")
    {
        var symbols = market == "US" ? MarketHelper.USDeepMovers : MarketHelper.IndianDeepMovers;
        
        // Retrieve batch quotes
        var allQuotes = await _marketDataService.GetQuotesAsync(symbols);

        // Sort for Top 10 by Market Cap
        var topByCap = allQuotes
            .OrderByDescending(x => x.MarketCap ?? 0)
            .Take(10)
            .ToList();

        // Sort for Top 10 by Change %
        var topByReturn = allQuotes
            .OrderByDescending(x => x.ChangePercent)
            .Take(10)
            .ToList();

        return Ok(new MarketMoversResponse(topByCap, topByReturn));
    }
}

public class SyncRequest
{
    public string Url { get; set; } = "https://www.amfiindia.com/spages/NAVAll.txt";
}
