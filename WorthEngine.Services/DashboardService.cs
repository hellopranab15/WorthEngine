using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;

namespace WorthEngine.Services;

public class DashboardService : IDashboardService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInsuranceRepository _insuranceRepository;
    private readonly IXirrService _xirrService;

    public DashboardService(IPortfolioRepository portfolioRepository, IInsuranceRepository insuranceRepository, IXirrService xirrService)
    {
        _portfolioRepository = portfolioRepository;
        _insuranceRepository = insuranceRepository;
        _xirrService = xirrService;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(string userId)
    {
        var portfolios = (await _portfolioRepository.GetByUserIdAsync(userId)).ToList();
        var insurances = (await _insuranceRepository.GetByUserIdAsync(userId)).ToList();

        // Calculate total net worth
        decimal totalNetWorth = portfolios.Sum(p => p.CurrentValue);

        // Calculate total invested
        decimal totalInvested = 0;
        
        foreach (var portfolio in portfolios)
        {
            if (portfolio.Type == "EPF")
            {
                // For EPF, invested amount = opening balances + all contributions
                var epfInvested = (portfolio.OpeningEmployeeBalance ?? 0) + 
                                  (portfolio.OpeningEmployerBalance ?? 0);
                
                if (portfolio.EpfContributions != null)
                {
                    epfInvested += portfolio.EpfContributions.Sum(c => c.EmployeeShare + c.EmployerShare);
                }
                
                totalInvested += epfInvested;
            }
            else
            {
                // For other types, use transactions
                var deposits = portfolio.Transactions
                    .Where(t => t.Type == "DEPOSIT" || t.Type == "BUY")
                    .Sum(t => t.Amount);
                var withdrawals = portfolio.Transactions
                    .Where(t => t.Type == "WITHDRAWAL" || t.Type == "SELL")
                    .Sum(t => t.Amount);
                    
                totalInvested += (deposits - withdrawals);
            }
        }

        decimal totalGain = totalNetWorth - totalInvested;
        decimal gainPercentage = totalInvested > 0 ? (totalGain / totalInvested) * 100 : 0;

        // Separate portfolios by type
        var stockPortfolios = portfolios.Where(p => p.Type == "STOCK").ToList();
        var mfPortfolios = portfolios.Where(p => p.Type == "MF" || p.Type == "SIP").ToList();
        
        // Calculate Stock totals and XIRR
        decimal totalStockValue = stockPortfolios.Sum(p => p.CurrentValue);
        decimal? stockXirr = null;
        if (stockPortfolios.Any())
        {
            var stockTransactions = stockPortfolios.SelectMany(p => p.Transactions).ToList();
            if (stockTransactions.Any() && totalStockValue > 0)
            {
                try
                {
                    var xirrResult = _xirrService.CalculateXirr(stockTransactions, totalStockValue);
                    stockXirr = xirrResult.Xirr;
                }
                catch { /* Ignore calculation errors */ }
            }
        }

        // Calculate MF totals and XIRR
        decimal totalMfValue = mfPortfolios.Sum(p => p.CurrentValue);
        decimal? mfXirr = null;
        if (mfPortfolios.Any())
        {
            var mfTransactions = mfPortfolios.SelectMany(p => p.Transactions).ToList();
            if (mfTransactions.Any() && totalMfValue > 0)
            {
                try
                {
                    var xirrResult = _xirrService.CalculateXirr(mfTransactions, totalMfValue);
                    mfXirr = xirrResult.Xirr;
                }
                catch { /* Ignore calculation errors */ }
            }
        }

        // Calculate EPF totals
        var epfPortfolios = portfolios.Where(p => p.Type == "EPF").ToList();
        decimal totalEpfValue = epfPortfolios.Sum(p => p.CurrentValue);

        // Calculate Overall XIRR (Stock + MF only, excluding EPF, NPS, SAVING)
        decimal? overallXirr = null;
        var investablePortfolios = portfolios.Where(p => p.Type == "STOCK" || p.Type == "MF" || p.Type == "SIP").ToList();
        if (investablePortfolios.Any())
        {
            var allInvestableTransactions = investablePortfolios.SelectMany(p => p.Transactions).ToList();
            var totalInvestableValue = investablePortfolios.Sum(p => p.CurrentValue);
            if (allInvestableTransactions.Any() && totalInvestableValue > 0)
            {
                try
                {
                    var xirrResult = _xirrService.CalculateXirr(allInvestableTransactions, totalInvestableValue);
                    overallXirr = xirrResult.Xirr;
                }
                catch { /* Ignore calculation errors */ }
            }
        }

        // Asset allocation by type
        var assetAllocations = portfolios
            .GroupBy(p => p.Type)
            .Select(g => new AssetAllocation(
                Type: g.Key,
                Value: g.Sum(p => p.CurrentValue),
                Percentage: totalNetWorth > 0 ? Math.Round((g.Sum(p => p.CurrentValue) / totalNetWorth) * 100, 2) : 0
            ))
            .ToList();

        // Insurance alerts (due within 30 days)
        var insuranceAlerts = insurances
            .Select(i => new InsuranceAlert(
                PolicyName: i.PolicyName,
                Type: i.Type,
                DueDate: i.PremiumDueDate,
                Premium: i.PremiumAmount,
                IsDueSoon: i.IsDueSoon
            ))
            .OrderBy(a => a.DueDate)
            .ToList();

        return new DashboardSummary(
            TotalNetWorth: totalNetWorth,
            AssetAllocations: assetAllocations,
            InsuranceAlerts: insuranceAlerts,
            TotalInvested: totalInvested,
            TotalGain: totalGain,
            GainPercentage: Math.Round(gainPercentage, 2),
            OverallXirr: overallXirr,
            StockXirr: stockXirr,
            MfXirr: mfXirr,
            TotalStockValue: totalStockValue,
            TotalMfValue: totalMfValue,
            TotalEpfValue: totalEpfValue
        );
    }
}
