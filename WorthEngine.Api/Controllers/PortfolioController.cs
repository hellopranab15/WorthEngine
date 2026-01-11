using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;

namespace WorthEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly IXirrService _xirrService;
    private readonly IPortfolioRepository _portfolioRepository;

    public PortfolioController(
        IPortfolioService portfolioService, 
        IXirrService xirrService,
        IPortfolioRepository portfolioRepository)
    {
        _portfolioService = portfolioService;
        _xirrService = xirrService;
        _portfolioRepository = portfolioRepository;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PortfolioResponse>>> GetAll()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);
        return Ok(portfolios);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PortfolioResponse>> GetById(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var portfolio = await _portfolioService.GetPortfolioAsync(id, userId);
        if (portfolio == null)
            return NotFound();

        return Ok(portfolio);
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioResponse>> Create([FromBody] PortfolioRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var portfolio = await _portfolioService.CreatePortfolioAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = portfolio.Id }, portfolio);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PortfolioResponse>> Update(string id, [FromBody] PortfolioRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try 
        {
            var portfolio = await _portfolioService.UpdatePortfolioAsync(id, userId, request);
            return Ok(portfolio);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/transaction")]
    public async Task<ActionResult<PortfolioResponse>> AddTransaction(string id, [FromBody] TransactionRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var portfolio = await _portfolioService.AddTransactionAsync(id, userId, request);
            return Ok(portfolio);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _portfolioService.DeletePortfolioAsync(id, userId);
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshPrices()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _portfolioService.RefreshPricesAsync(userId);
        return Ok(new { message = "Prices refreshed successfully" });
    }

    [HttpGet("{id}/xirr")]
    public async Task<ActionResult<XirrResult>> GetXirr(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            return NotFound();

        var xirr = _xirrService.CalculateXirr(portfolio.Transactions, portfolio.CurrentValue);
        return Ok(xirr);
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<List<TransactionDetailResponse>>> GetTransactions(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var transactions = await _portfolioService.GetPortfolioTransactionsAsync(id, userId);
            return Ok(transactions);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/transaction/{transactionIndex}")]
    public async Task<ActionResult<PortfolioResponse>> UpdateTransaction(string id, int transactionIndex, [FromBody] TransactionRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var portfolio = await _portfolioService.UpdateTransactionAsync(id, userId, transactionIndex, request);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // EPF Endpoints
    [HttpPost("epf/setup")]
    public async Task<ActionResult<PortfolioResponse>> SetupEpf([FromBody] EpfSetupRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var portfolio = await _portfolioService.SetupEpfAsync(userId, request);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/epf/summary")]
    public async Task<ActionResult<EpfSummaryResponse>> GetEpfSummary(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var summary = await _portfolioService.GetEpfSummaryAsync(id, userId);
            return Ok(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/epf/basic-pay")]
    public async Task<ActionResult<PortfolioResponse>> UpdateBasicPay(string id, [FromBody] UpdateBasicPayRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var portfolio = await _portfolioService.UpdateEpfBasicPayAsync(id, userId, request);
            return Ok(portfolio);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
        [HttpPost("{id}/fix-transactions")]
    public async Task<ActionResult<PortfolioResponse>> FixTransactions(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var portfolio = await _portfolioService.FixPortfolioTransactionsAsync(id, userId);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/stock-price")]
    public async Task<ActionResult<StockPriceResponse>> GetStockPrice(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var stockPrice = await _portfolioService.GetStockPriceAsync(id, userId);
            return Ok(stockPrice);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
