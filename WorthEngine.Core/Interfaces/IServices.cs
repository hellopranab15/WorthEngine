using WorthEngine.Core.DTOs;
using WorthEngine.Core.Models;

namespace WorthEngine.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    string GenerateJwtToken(User user);
}

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioResponse>> GetUserPortfoliosAsync(string userId);
    Task<PortfolioResponse?> GetPortfolioAsync(string id, string userId);
    Task<PortfolioResponse> CreatePortfolioAsync(string userId, PortfolioRequest request);
    Task<PortfolioResponse> UpdatePortfolioAsync(string id, string userId, PortfolioRequest request);
    Task DeletePortfolioAsync(string id, string userId);
    Task RefreshPricesAsync(string userId);
    Task<PortfolioResponse> AddTransactionAsync(string id, string userId, TransactionRequest request);
    Task<List<TransactionDetailResponse>> GetPortfolioTransactionsAsync(string id, string userId);
    Task<PortfolioResponse> UpdateTransactionAsync(string id, string userId, int transactionIndex, TransactionRequest request);
    Task<PortfolioResponse> FixPortfolioTransactionsAsync(string id, string userId);
    Task<StockPriceResponse> GetStockPriceAsync(string id, string userId);
    Task RecalculateAllXirrAsync(string userId);
    
    // EPF methods
    Task<PortfolioResponse> SetupEpfAsync(string userId, EpfSetupRequest request);
    Task<EpfSummaryResponse> GetEpfSummaryAsync(string portfolioId, string userId);
    Task<PortfolioResponse> UpdateEpfBasicPayAsync(string portfolioId, string userId, UpdateBasicPayRequest request);
}

public interface IInsuranceService
{
    Task<IEnumerable<InsuranceResponse>> GetUserInsurancesAsync(string userId);
    Task<InsuranceResponse> CreateInsuranceAsync(string userId, InsuranceRequest request);
    Task DeleteInsuranceAsync(string id, string userId);
}

public interface IDashboardService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(string userId);
}


public interface IXirrService
{
    XirrResult CalculateXirr(List<Transaction> transactions, decimal currentValue);
}

public interface IMarketDataService
{
    Task<decimal?> GetMutualFundNavAsync(string schemeCode);
    Task<MfDetailsResponse?> GetMfDetailsAsync(string schemeCode);
    Task<decimal?> GetStockPriceAsync(string tickerSymbol);
    Task<(string? companyName, string? sector, long? marketCap)> GetStockMetadataAsync(string tickerSymbol);
    Task<List<StockSearchResult>> SearchStocksAsync(string query);
    Task<List<StockPriceResponse>> GetQuotesAsync(List<string> symbols);
    Task SyncAmfiDataAsync(string url);
    Task<IEnumerable<MutualFundScheme>> SearchSchemesAsync(string query);
}
