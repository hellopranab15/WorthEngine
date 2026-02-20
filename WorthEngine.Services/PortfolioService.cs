using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly IXirrService _xirrService;

    public PortfolioService(IPortfolioRepository portfolioRepository, IMarketDataService marketDataService, IXirrService xirrService)
    {
        _portfolioRepository = portfolioRepository;
        _marketDataService = marketDataService;
        _xirrService = xirrService;
    }

    public async Task<IEnumerable<PortfolioResponse>> GetUserPortfoliosAsync(string userId)
    {
        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
        var responses = new List<PortfolioResponse>();

        foreach (var portfolio in portfolios)
        {
            // For stocks and mutual funds, fetch live prices
            if ((portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol)) ||
                ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode)))
            {
                try
                {
                    decimal? livePrice = null;

                    // Fetch live price based on type
                    if (portfolio.Type == "STOCK")
                    {
                        livePrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol!);
                    }
                    else if (portfolio.Type == "MF" || portfolio.Type == "SIP")
                    {
                        livePrice = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode!);
                    }

                    if (livePrice.HasValue && portfolio.UnitsHeld > 0)
                    {
                        // Calculate current value with live price
                        var newCurrentValue = portfolio.UnitsHeld * livePrice.Value;

                        // Recalculate XIRR and gains with live data
                        decimal xirrValue = 0;
                        decimal gain = 0;
                        decimal gainPercentage = 0;

                        if (portfolio.Transactions != null && portfolio.Transactions.Any())
                        {
                            var xirrResult = _xirrService.CalculateXirr(portfolio.Transactions.ToList(), newCurrentValue);
                            xirrValue = xirrResult.Xirr;
                            gain = xirrResult.CurrentValue - xirrResult.InvestedAmount;
                            gainPercentage = xirrResult.InvestedAmount > 0
                                ? (gain / xirrResult.InvestedAmount) * 100
                                : 0;
                        }

                        // Return response with live calculated values (don't update DB)
                        responses.Add(new PortfolioResponse(
                            portfolio.Id,
                            portfolio.Type,
                            portfolio.ProviderName,
                            portfolio.SchemeCode,
                            portfolio.TickerSymbol,
                            portfolio.SipStartDate,
                            portfolio.SipDeductionDay,
                            portfolio.UnitsHeld,
                            newCurrentValue,
                            portfolio.PurchasePrice,
                            gain,
                            gainPercentage,
                            xirrValue,
                            portfolio.LastUpdated
                        ));
                        continue;
                    }
                }
                catch
                {
                    // If live price fetch fails, fall through to use stored values
                }
            }

            // For other types or if live price fetch failed, use stored values
            responses.Add(MapToResponse(portfolio));
        }

        return responses;
    }

    public async Task<PortfolioResponse?> GetPortfolioAsync(string id, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            return null;

        // For stocks and mutual funds, update current value with live price and recalculate XIRR
        if (portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol))
        {
            try
            {
                var livePrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
                if (livePrice.HasValue)
                {
                    // Update current value with live price
                    var newCurrentValue = portfolio.UnitsHeld * livePrice.Value;
                    
                    // Recalculate XIRR with live current value
                    decimal xirrValue = 0;
                    decimal gain = 0;
                    decimal gainPercentage = 0;
                    
                    if (portfolio.Transactions != null && portfolio.Transactions.Any())
                    {
                        var xirrResult = _xirrService.CalculateXirr(portfolio.Transactions.ToList(), newCurrentValue);
                        xirrValue = xirrResult.Xirr;
                        gain = xirrResult.CurrentValue - xirrResult.InvestedAmount;
                        gainPercentage = xirrResult.InvestedAmount > 0 
                            ? (gain / xirrResult.InvestedAmount) * 100 
                            : 0;
                    }
                    
                    // Return response with recalculated values
                    return new PortfolioResponse(
                        portfolio.Id,
                        portfolio.Type,
                        portfolio.ProviderName,
                        portfolio.SchemeCode,
                        portfolio.TickerSymbol,
                        portfolio.SipStartDate,
                        portfolio.SipDeductionDay,
                        portfolio.UnitsHeld,
                        newCurrentValue,
                        portfolio.PurchasePrice,
                        gain,
                        gainPercentage,
                        xirrValue,
                        portfolio.LastUpdated
                    );
                }
            }
            catch
            {
                // If live price fetch fails, use stored values
            }
        }
        else if ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode))
        {
            try
            {
                var liveNav = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
                if (liveNav.HasValue)
                {
                    // Update current value with live NAV
                    var newCurrentValue = portfolio.UnitsHeld * liveNav.Value;
                    
                    // Recalculate XIRR with live current value
                    decimal xirrValue = 0;
                    decimal gain = 0;
                    decimal gainPercentage = 0;
                    
                    if (portfolio.Transactions != null && portfolio.Transactions.Any())
                    {
                        var xirrResult = _xirrService.CalculateXirr(portfolio.Transactions.ToList(), newCurrentValue);
                        xirrValue = xirrResult.Xirr;
                        gain = xirrResult.CurrentValue - xirrResult.InvestedAmount;
                        gainPercentage = xirrResult.InvestedAmount > 0 
                            ? (gain / xirrResult.InvestedAmount) * 100 
                            : 0;
                    }
                    
                    // Return response with recalculated values
                    return new PortfolioResponse(
                        portfolio.Id,
                        portfolio.Type,
                        portfolio.ProviderName,
                        portfolio.SchemeCode,
                        portfolio.TickerSymbol,
                        portfolio.SipStartDate,
                        portfolio.SipDeductionDay,
                        portfolio.UnitsHeld,
                        newCurrentValue,
                        portfolio.PurchasePrice,
                        gain,
                        gainPercentage,
                        xirrValue,
                        portfolio.LastUpdated
                    );
                }
            }
            catch
            {
                // If live NAV fetch fails, use stored values
            }
        }

        // For non-stocks/MF or if live price fetch failed, calculate from stored values
        decimal fallbackGain = 0;
        decimal fallbackGainPercentage = 0;
        decimal fallbackXirr = 0;
        
        if (portfolio.Transactions != null && portfolio.Transactions.Any())
        {
            var xirrResult = _xirrService.CalculateXirr(portfolio.Transactions.ToList(), portfolio.CurrentValue);
            fallbackXirr = xirrResult.Xirr;
            fallbackGain = xirrResult.CurrentValue - xirrResult.InvestedAmount;
            fallbackGainPercentage = xirrResult.InvestedAmount > 0 
                ? (fallbackGain / xirrResult.InvestedAmount) * 100 
                : 0;
        }
        
        return new PortfolioResponse(
            portfolio.Id,
            portfolio.Type,
            portfolio.ProviderName,
            portfolio.SchemeCode,
            portfolio.TickerSymbol,
            portfolio.SipStartDate,
            portfolio.SipDeductionDay,
            portfolio.UnitsHeld,
            portfolio.CurrentValue,
            portfolio.PurchasePrice,
            fallbackGain,
            fallbackGainPercentage,
            fallbackXirr,
            portfolio.LastUpdated
        );
    }

    public async Task<PortfolioResponse> CreatePortfolioAsync(string userId, PortfolioRequest request)
    {
        var portfolio = new Portfolio
        {
            UserId = userId,
            Type = request.Type,
            ProviderName = request.ProviderName,
            SchemeCode = request.SchemeCode,
            TickerSymbol = request.TickerSymbol,
            SipStartDate = request.SipStartDate,
            SipDeductionDay = request.SipDeductionDay,
            LastUpdated = DateTime.UtcNow,
            Transactions = request.Transactions?.Select(t => new Transaction
            {
                Date = t.Date,
                Amount = t.Amount,
                Type = t.Type,
                Units = t.Units,  // Copy units from request
                Price = t.Price,  // Copy price from request
                InvestmentDate = t.InvestmentDate ?? t.Date
            }).ToList() ?? new List<Transaction>()
        };

        // If transactions are provided, recalculate totals from them
        if (portfolio.Transactions.Any())
        {
            RecalculatePortfolioTotals(portfolio);
        }
        else
        {
            // No transactions provided, use the request values directly
            portfolio.UnitsHeld = request.UnitsHeld;
            portfolio.PurchasePrice = request.PurchasePrice;
            portfolio.CurrentValue = request.PurchasePrice * request.UnitsHeld;
            
            // Add initial transaction if purchase price > 0
            if (portfolio.PurchasePrice > 0 && portfolio.UnitsHeld > 0)
            {
                portfolio.Transactions.Add(new Transaction
                {
                    Date = DateTime.UtcNow,
                    Amount = portfolio.PurchasePrice * portfolio.UnitsHeld,
                    Type = "DEPOSIT",
                    Units = portfolio.UnitsHeld,
                    Price = portfolio.PurchasePrice
                });
            }
        }

        await _portfolioRepository.CreateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    public async Task DeletePortfolioAsync(string id, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio != null && portfolio.UserId == userId)
        {
            await _portfolioRepository.DeleteAsync(id);
        }
    }

    public async Task RefreshPricesAsync(string userId)
    {
        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);

        foreach (var portfolio in portfolios)
        {
            decimal? newPrice = null;

            if (!string.IsNullOrEmpty(portfolio.SchemeCode))
            {
                newPrice = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
            }
            else if (!string.IsNullOrEmpty(portfolio.TickerSymbol))
            {
                newPrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
            }

            if (newPrice.HasValue)
            {
                var newValue = portfolio.UnitsHeld * newPrice.Value;
                await _portfolioRepository.UpdateCurrentValueAsync(portfolio.Id, newValue, DateTime.UtcNow);
            }
        }
    }

    public async Task<PortfolioResponse> AddTransactionAsync(string id, string userId, TransactionRequest request)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        var transaction = new Transaction
        {
            Date = request.Date,
            Amount = request.Amount,
            Type = request.Type,
            InvestmentDate = request.InvestmentDate ?? request.Date,
            Units = request.Units,
            Price = request.Price
        };

        portfolio.Transactions.Add(transaction);

        // Fetch live price/NAV to calculate current value
        decimal? currentPrice = null;
        if (portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol))
        {
            try
            {
                currentPrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
            }
            catch
            {
                // If live price fetch fails, fall back to latest transaction price
            }
        }
        else if ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode))
        {
            try
            {
                currentPrice = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
            }
            catch
            {
                // If live NAV fetch fails, fall back to latest transaction NAV
            }
        }

        // Recalculate portfolio totals with current price/NAV
        RecalculatePortfolioTotals(portfolio, currentPrice);
        
        portfolio.LastUpdated = DateTime.UtcNow;

        await _portfolioRepository.UpdateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    private void RecalculatePortfolioTotals(Portfolio portfolio, decimal? overrideCurrentPrice = null)
    {
        // 1. Calculate total units from all transactions
        decimal totalUnits = 0;
        bool hasUnitTransactions = false;

        if (portfolio.Transactions != null)
        {
            foreach (var txn in portfolio.Transactions)
            {
                if (txn.Units.HasValue)
                {
                    hasUnitTransactions = true;
                    if (txn.Type == "DEPOSIT" || txn.Type == "BUY")
                        totalUnits += txn.Units.Value;
                    else if (txn.Type == "WITHDRAWAL" || txn.Type == "SELL")
                        totalUnits -= txn.Units.Value;
                }
            }
        }
        
        // Only update UnitsHeld if we have valid transactions with units
        if (hasUnitTransactions)
        {
            portfolio.UnitsHeld = totalUnits;
        }

        // 2. Calculate total invested amount (buy transactions - sell transactions)
        // Only consider if transactions exist
        if (portfolio.Transactions != null && portfolio.Transactions.Any())
        {
            var totalBuyAmount = portfolio.Transactions
                .Where(t => t.Type == "DEPOSIT" || t.Type == "BUY")
                .Sum(t => t.Amount);
            
            var totalSellAmount = portfolio.Transactions
                .Where(t => t.Type == "WITHDRAWAL" || t.Type == "SELL")
                .Sum(t => t.Amount);

            var netInvested = totalBuyAmount - totalSellAmount;

            // 3. Calculate average purchase price
            if (portfolio.UnitsHeld > 0 && netInvested > 0)
            {
                portfolio.PurchasePrice = netInvested / portfolio.UnitsHeld;
            }
            else if (portfolio.UnitsHeld == 0)
            {
                portfolio.PurchasePrice = 0;
            }
        }

        // 4. Update current value based on override OR latest price
        if (overrideCurrentPrice.HasValue && portfolio.UnitsHeld > 0)
        {
            portfolio.CurrentValue = portfolio.UnitsHeld * overrideCurrentPrice.Value;
        }
        else if (portfolio.Transactions != null) 
        {
            var latestTransaction = portfolio.Transactions
                .Where(t => t.Price.HasValue)
                .OrderByDescending(t => t.Date)
                .FirstOrDefault();

            if (latestTransaction != null && latestTransaction.Price.HasValue && portfolio.UnitsHeld > 0)
            {
                portfolio.CurrentValue = portfolio.UnitsHeld * latestTransaction.Price.Value;
            }
        }
    }

    public async Task<PortfolioResponse> UpdatePortfolioAsync(string id, string userId, PortfolioRequest request)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        portfolio.ProviderName = request.ProviderName;
        portfolio.SchemeCode = request.SchemeCode;
        portfolio.TickerSymbol = request.TickerSymbol;
        portfolio.UnitsHeld = request.UnitsHeld;
        portfolio.PurchasePrice = request.PurchasePrice; // Weighted average or latest
        portfolio.CurrentValue = request.PurchasePrice * request.UnitsHeld; // Or derived
        
        // Handle type conversion: Clear SIP-specific fields when converting to MF (Lumpsum)
        portfolio.Type = request.Type;
        if (request.Type == "MF")
        {
            // Converting to Lumpsum - clear SIP fields
            portfolio.SipStartDate = null;
            portfolio.SipDeductionDay = null;
        }
        else if (request.Type == "SIP")
        {
            // Keep or update SIP fields
            portfolio.SipStartDate = request.SipStartDate;
            portfolio.SipDeductionDay = request.SipDeductionDay;
        }
        
        portfolio.LastUpdated = DateTime.UtcNow;

        await _portfolioRepository.UpdateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    public async Task<List<TransactionDetailResponse>> GetPortfolioTransactionsAsync(string id, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        // For stocks and mutual funds, fetch live price/NAV; for others use calculated NAV
        decimal currentNav = 0;
        if (portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol))
        {
            var livePrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
            currentNav = livePrice ?? (portfolio.UnitsHeld > 0 ? portfolio.CurrentValue / portfolio.UnitsHeld : 0);
        }
        else if ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode))
        {
            var liveNav = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
            currentNav = liveNav ?? (portfolio.UnitsHeld > 0 ? portfolio.CurrentValue / portfolio.UnitsHeld : 0);
        }
        else
        {
            currentNav = portfolio.UnitsHeld > 0 ? portfolio.CurrentValue / portfolio.UnitsHeld : 0;
        }

        var transactionDetails = new List<TransactionDetailResponse>();

        foreach (var transaction in portfolio.Transactions)
        {
            decimal? transactionXirr = null;
            decimal? currentValue = null;
            decimal? returns = null;

            // Calculate individual XIRR for this transaction
            if (transaction.Type == "DEPOSIT" || transaction.Type == "BUY")
            {
                try
                {
                    // For individual transaction XIRR: investment on investmentDate, current value today
                    var investmentDate = transaction.InvestmentDate ?? transaction.Date;
                    var transactionList = new List<Transaction>
                    {
                        new Transaction
                        {
                            Date = investmentDate,
                            Amount = transaction.Amount,
                            Type = "DEPOSIT"
                        }
                    };

                    // Calculate current value for this transaction's units
                    decimal transactionUnits = 0;
                    if (transaction.Units.HasValue && transaction.Units.Value > 0)
                    {
                        // Use stored units from transaction
                        transactionUnits = transaction.Units.Value;
                        currentValue = transactionUnits * currentNav;
                        
                        // Calculate returns
                        returns = currentValue - transaction.Amount;
                    }
                    else if (transaction.Amount > 0 && currentNav > 0)
                    {
                        // Estimate units from amount and purchase price
                        var estimatedPurchasePrice = transaction.Price ?? portfolio.PurchasePrice;
                        if (estimatedPurchasePrice > 0)
                        {
                            transactionUnits = transaction.Amount / estimatedPurchasePrice;
                            currentValue = transactionUnits * currentNav;
                            returns = currentValue - transaction.Amount;
                        }
                    }

                    if (currentValue > 0)
                    {
                        var xirrResult = _xirrService.CalculateXirr(transactionList, currentValue.Value);
                        transactionXirr = xirrResult.Xirr;
                    }
                }
                catch { }
            }

            transactionDetails.Add(new TransactionDetailResponse(
                transaction.Date,
                transaction.InvestmentDate,
                transaction.Amount,
                transaction.Type,
                transaction.Units, // Return stored units
                transaction.Price, // Return stored price
                currentValue,
                returns,
                transactionXirr
            ));
        }

        return transactionDetails;
    }

    public async Task<PortfolioResponse> UpdateTransactionAsync(string id, string userId, int transactionIndex, TransactionRequest request)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        if (transactionIndex < 0 || transactionIndex >= portfolio.Transactions.Count)
            throw new Exception("Invalid transaction index");

        // Update the transaction
        portfolio.Transactions[transactionIndex].Date = request.Date;
        portfolio.Transactions[transactionIndex].Amount = request.Amount;
        portfolio.Transactions[transactionIndex].Type = request.Type;
        portfolio.Transactions[transactionIndex].InvestmentDate = request.InvestmentDate ?? request.Date;
        portfolio.Transactions[transactionIndex].Units = request.Units;
        portfolio.Transactions[transactionIndex].Price = request.Price;

        // Fetch live price/NAV to calculate current value
        decimal? currentPrice = null;
        if (portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol))
        {
            try
            {
                currentPrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
            }
            catch
            {
                // If live price fetch fails, fall back to latest transaction price
            }
        }
        else if ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode))
        {
            try
            {
                currentPrice = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
            }
            catch
            {
                // If live NAV fetch fails, fall back to latest transaction NAV
            }
        }

        // Recalculate portfolio totals with current price/NAV
        RecalculatePortfolioTotals(portfolio, currentPrice);

        portfolio.LastUpdated = DateTime.UtcNow;

        await _portfolioRepository.UpdateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    private PortfolioResponse MapToResponse(Portfolio p)
    {
        var invested = p.Transactions
            .Where(t => t.Type == "DEPOSIT" || t.Type == "BUY")
            .Sum(t => t.Amount) - p.Transactions
            .Where(t => t.Type == "WITHDRAWAL" || t.Type == "SELL")
            .Sum(t => t.Amount);

        var gain = p.CurrentValue - invested;
        var gainPercentage = invested > 0 ? (gain / invested) * 100 : 0;

        // Calculate XIRR only for Stock, MF, and SIP (not for EPF, NPS, SAVING)
        decimal? xirr = null;
        if ((p.Type == "STOCK" || p.Type == "MF" || p.Type == "SIP") && 
            p.Transactions.Any() && p.CurrentValue > 0)
        {
            try
            {
                var result = _xirrService.CalculateXirr(p.Transactions, p.CurrentValue);
                xirr = result.Xirr;
            }
            catch { }
        }

        return new PortfolioResponse(
            p.Id,
            p.Type,
            p.ProviderName,
            p.SchemeCode,
            p.TickerSymbol,
            p.SipStartDate,
            p.SipDeductionDay,
            p.UnitsHeld,
            p.CurrentValue,
            p.PurchasePrice,
            gain,
            Math.Round(gainPercentage, 2),
            xirr,
            p.LastUpdated
        );
    }

    // EPF Methods
    public async Task<PortfolioResponse> SetupEpfAsync(string userId, EpfSetupRequest request)
    {
        var portfolio = new Portfolio
        {
            UserId = userId,
            Type = "EPF",
            ProviderName = request.ProviderName,
            OpeningEmployeeBalance = request.OpeningEmployeeBalance,
            OpeningEmployerBalance = request.OpeningEmployerBalance,
            EpfBasicPay = request.BasicPay,
            IsEpsMember = request.IsEpsMember,
            EpfInterestRate = request.InterestRate,
            EpfContributions = new List<EpfContribution>(),
            Transactions = new List<Transaction>(),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Generate contributions from April to current month
        GenerateEpfContributions(portfolio, request.FinancialYear);

        // Calculate current value
        CalculateEpfCurrentValue(portfolio);

        await _portfolioRepository.CreateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    private void GenerateEpfContributions(Portfolio portfolio, int financialYear)
    {
        var startMonth = new DateTime(financialYear, 4, 1); // April
        var currentMonth = DateTime.Now;

        var month = startMonth;
        while (month <= currentMonth)
        {
            decimal employeeShare = portfolio.EpfBasicPay!.Value * 0.12m; // 12% employee contribution
            
            decimal epsContribution = 0;
            decimal employerShare = 0;
            decimal epsWage = 0;

            if (portfolio.IsEpsMember!.Value)
            {
                // EPS contribution is 8.33% of wage, capped at ₹1,250 (when wage ceiling is ₹15,000)
                epsWage = Math.Min(portfolio.EpfBasicPay.Value, 15000);
                epsContribution = Math.Min(epsWage * 0.0833m, 1250);
                
                // Employer EPF share = 12% - EPS contribution
                employerShare = (portfolio.EpfBasicPay.Value * 0.12m) - epsContribution;
            }
            else
            {
                // If not EPS member, full 12% goes to EPF
                employerShare = portfolio.EpfBasicPay.Value * 0.12m;
            }

            var contribution = new EpfContribution
            {
                Month = month,
                EpfWage = portfolio.EpfBasicPay.Value,
                EmployeeShare = employeeShare,
                EmployerShare = employerShare,
                EpsWage = epsWage
            };

            portfolio.EpfContributions!.Add(contribution);
            month = month.AddMonths(1);
        }
    }

    private void CalculateEpfCurrentValue(Portfolio portfolio)
    {
        var totalEmployeeContribution = portfolio.EpfContributions!.Sum(c => c.EmployeeShare);
        var totalEmployerContribution = portfolio.EpfContributions.Sum(c => c.EmployerShare);

        var totalContribution = totalEmployeeContribution + totalEmployerContribution;
        var openingBalance = portfolio.OpeningEmployeeBalance!.Value + portfolio.OpeningEmployerBalance!.Value;

        // Simple interest calculation (monthly compounding)
        var monthsElapsed = portfolio.EpfContributions.Count;
        var annualInterestRate = portfolio.EpfInterestRate!.Value / 100;
        var monthlyInterestRate = annualInterestRate / 12;

        // Calculate interest on opening balance + contributions
        var interest = (openingBalance + totalContribution) * annualInterestRate * (monthsElapsed / 12m);

        portfolio.CurrentValue = openingBalance + totalContribution + interest;
        portfolio.UnitsHeld = 0; // Not applicable for EPF
        portfolio.PurchasePrice = 0; // Not applicable for EPF
    }

    public async Task<EpfSummaryResponse> GetEpfSummaryAsync(string portfolioId, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);

        if (portfolio == null || portfolio.UserId != userId)
            throw new UnauthorizedAccessException("Portfolio not found or access denied");

        // Auto-extend contributions to current month if behind
        if (portfolio.EpfContributions != null && portfolio.EpfContributions.Any())
        {
            var lastContribution = portfolio.EpfContributions.Max(c => c.Month);
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            if (lastContribution < currentMonth)
            {
                var nextMonth = lastContribution.AddMonths(1);
                while (nextMonth <= currentMonth)
                {
                    decimal employeeShare = portfolio.EpfBasicPay!.Value * 0.12m;
                    decimal epsContribution = 0;
                    decimal employerShare = 0;
                    decimal epsWage = 0;

                    if (portfolio.IsEpsMember!.Value)
                    {
                        epsWage = Math.Min(portfolio.EpfBasicPay.Value, 15000);
                        epsContribution = Math.Min(epsWage * 0.0833m, 1250);
                        employerShare = (portfolio.EpfBasicPay.Value * 0.12m) - epsContribution;
                    }
                    else
                    {
                        employerShare = portfolio.EpfBasicPay.Value * 0.12m;
                    }

                    portfolio.EpfContributions.Add(new EpfContribution
                    {
                        Month = nextMonth,
                        EpfWage = portfolio.EpfBasicPay.Value,
                        EmployeeShare = employeeShare,
                        EmployerShare = employerShare,
                        EpsWage = epsWage
                    });

                    nextMonth = nextMonth.AddMonths(1);
                }

                // Recalculate current value with new contributions
                CalculateEpfCurrentValue(portfolio);
                portfolio.LastUpdated = DateTime.UtcNow;
                await _portfolioRepository.UpdateAsync(portfolio);
            }
        }

        var totalEmployeeContribution = portfolio.EpfContributions!.Sum(c => c.EmployeeShare);
        var totalEmployerContribution = portfolio.EpfContributions.Sum(c => c.EmployerShare);

        var totalContribution = totalEmployeeContribution + totalEmployerContribution;
        var openingBalance = portfolio.OpeningEmployeeBalance!.Value + portfolio.OpeningEmployerBalance!.Value;

        var monthsElapsed = portfolio.EpfContributions.Count;
        var annualInterestRate = portfolio.EpfInterestRate!.Value / 100;
        var interest = (openingBalance + totalContribution) * annualInterestRate * (monthsElapsed / 12m);

        var currentEmployeeValue = portfolio.OpeningEmployeeBalance.Value + totalEmployeeContribution + (interest / 2);
        var currentEmployerValue = portfolio.OpeningEmployerBalance.Value + totalEmployerContribution + (interest / 2);

        return new EpfSummaryResponse(
            portfolio.Id,
            portfolio.ProviderName,
            portfolio.OpeningEmployeeBalance.Value,
            portfolio.OpeningEmployerBalance.Value,
            totalEmployeeContribution,
            totalEmployerContribution,
            currentEmployeeValue,
            currentEmployerValue,
            portfolio.CurrentValue,
            interest,
            portfolio.EpfInterestRate.Value,
            portfolio.IsEpsMember!.Value,
            portfolio.EpfBasicPay!.Value,
            portfolio.EpfContributions.Select(c => new EpfContributionResponse(
                c.Month,
                c.EpfWage,
                c.EmployeeShare,
                c.EmployerShare,
                c.EpsWage
            )).ToList()
        );
    }

    public async Task<PortfolioResponse> UpdateEpfBasicPayAsync(string portfolioId, string userId, UpdateBasicPayRequest request)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);

        if (portfolio == null || portfolio.UserId != userId)
            throw new UnauthorizedAccessException("Portfolio not found or access denied");

        portfolio.EpfBasicPay = request.NewBasicPay;

        // Update contributions from effective date onwards
        foreach (var contribution in portfolio.EpfContributions!.Where(c => c.Month >= request.EffectiveFrom))
        {
            contribution.EpfWage = request.NewBasicPay;
            contribution.EmployeeShare = request.NewBasicPay * 0.12m;
            
            if (portfolio.IsEpsMember!.Value)
            {
                // EPS contribution is 8.33% of wage, capped at ₹1,250
                decimal epsWage = Math.Min(request.NewBasicPay, 15000);
                decimal epsContribution = Math.Min(epsWage * 0.0833m, 1250);
                
                // Employer EPF share = 12% - EPS contribution
                contribution.EmployerShare = (request.NewBasicPay * 0.12m) - epsContribution;
                contribution.EpsWage = epsWage;
            }
            else
            {
                contribution.EmployerShare = request.NewBasicPay * 0.12m;
                contribution.EpsWage = 0;
            }
        }

        CalculateEpfCurrentValue(portfolio);
        portfolio.LastUpdated = DateTime.UtcNow;

        await _portfolioRepository.UpdateAsync(portfolio);
        return MapToResponse(portfolio);
    }

    public async Task<PortfolioResponse> FixPortfolioTransactionsAsync(string id, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        // Fix transactions that are missing Units and Price
        bool needsUpdate = false;
        foreach (var transaction in portfolio.Transactions)
        {
            // If Units or Price is missing, try to estimate
            if (!transaction.Units.HasValue || !transaction.Price.HasValue)
            {
                // If we have the portfolio's average price, use it to estimate
                if (portfolio.PurchasePrice > 0 && transaction.Amount > 0)
                {
                    // Estimate units from amount and average price
                    transaction.Units = transaction.Amount / portfolio.PurchasePrice;
                    transaction.Price = portfolio.PurchasePrice;
                    needsUpdate = true;
                }
            }
        }

        if (needsUpdate)
        {
            // Recalculate totals with the fixed data
            RecalculatePortfolioTotals(portfolio);
            portfolio.LastUpdated = DateTime.UtcNow;
            await _portfolioRepository.UpdateAsync(portfolio);
        }

        return MapToResponse(portfolio);
    }

    public async Task<StockPriceResponse> GetStockPriceAsync(string id, string userId)
    {
        var portfolio = await _portfolioRepository.GetByIdAsync(id);
        if (portfolio == null || portfolio.UserId != userId)
            throw new Exception("Portfolio not found");

        if (string.IsNullOrEmpty(portfolio.TickerSymbol))
            throw new Exception("No ticker symbol configured for this portfolio");

        // Fetch live price from Yahoo Finance
        var currentPrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
        
        if (!currentPrice.HasValue)
            throw new Exception("Failed to fetch current stock price");

        // Fetch stock metadata (company name, sector, market cap)
        var (companyName, sector, marketCap) = await _marketDataService.GetStockMetadataAsync(portfolio.TickerSymbol);

        // Calculate current value
        var currentValue = portfolio.UnitsHeld * currentPrice.Value;

        // Calculate change percentage from purchase price
        var changePercent = portfolio.PurchasePrice > 0 
            ? ((currentPrice.Value - portfolio.PurchasePrice) / portfolio.PurchasePrice) * 100 
            : 0;

        return new StockPriceResponse(
            portfolio.TickerSymbol,
            currentPrice.Value,
            currentValue,
            changePercent,
            DateTime.UtcNow,
            companyName,
            sector,
            marketCap
        );
    }

    public async Task RecalculateAllXirrAsync(string userId)
    {
        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);

        foreach (var portfolio in portfolios)
        {
            // Only recalculate for stocks and mutual funds
            if (portfolio.Type != "STOCK" && portfolio.Type != "MF" && portfolio.Type != "SIP")
                continue;

            try
            {
                decimal? livePrice = null;

                // Fetch live price based on type
                if (portfolio.Type == "STOCK" && !string.IsNullOrEmpty(portfolio.TickerSymbol))
                {
                    livePrice = await _marketDataService.GetStockPriceAsync(portfolio.TickerSymbol);
                }
                else if ((portfolio.Type == "MF" || portfolio.Type == "SIP") && !string.IsNullOrEmpty(portfolio.SchemeCode))
                {
                    livePrice = await _marketDataService.GetMutualFundNavAsync(portfolio.SchemeCode);
                }

                if (livePrice.HasValue)
                {
                    // Update current value with live price
                    portfolio.CurrentValue = portfolio.UnitsHeld * livePrice.Value;

                    // Recalculate XIRR if transactions exist
                    if (portfolio.Transactions != null && portfolio.Transactions.Any())
                    {
                        RecalculatePortfolioTotals(portfolio, livePrice);
                    }
                    else
                    {
                         // If no transactions but we have units, just update current value
                         portfolio.CurrentValue = portfolio.UnitsHeld * livePrice.Value;
                    }

                    portfolio.LastUpdated = DateTime.UtcNow;
                    // Use safer UpdateCurrentValueAsync so we don't accidentally overwrite UnitsHeld or other fields
                    await _portfolioRepository.UpdateCurrentValueAsync(portfolio.Id, portfolio.CurrentValue, portfolio.LastUpdated);
                }
            }
            catch
            {
                // Skip this portfolio if price fetch fails
                continue;
            }
        }
    }
}
