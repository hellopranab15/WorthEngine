namespace WorthEngine.Core.DTOs;

// Auth DTOs
public record RegisterRequest(string Username, string Password, string? Email);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, string Username, DateTime ExpiresAt);

// Dashboard DTOs
public record DashboardSummary(
    decimal TotalNetWorth,
    List<AssetAllocation> AssetAllocations,
    List<InsuranceAlert> InsuranceAlerts,
    decimal TotalInvested,
    decimal TotalGain,
    decimal GainPercentage,
    decimal? OverallXirr,
    decimal? StockXirr,
    decimal? MfXirr,
    decimal TotalStockValue,
    decimal TotalMfValue,
    decimal TotalEpfValue
);

public record AssetAllocation(string Type, decimal Value, decimal Percentage);
public record InsuranceAlert(string PolicyName, string Type, DateTime DueDate, decimal Premium, bool IsDueSoon);

// Portfolio DTOs
public record PortfolioRequest(
    string Type,
    string ProviderName,
    string? SchemeCode,
    string? TickerSymbol,
    DateTime? SipStartDate,
    int? SipDeductionDay,
    decimal UnitsHeld,
    decimal PurchasePrice,
    List<TransactionRequest>? Transactions
);

public record TransactionRequest(DateTime Date, decimal Amount, string Type, decimal? Units = null, decimal? Price = null, DateTime? InvestmentDate = null);

public record MfDetailsResponse(string SchemeCode, string SchemeName, decimal CurrentNav, DateTime Date);

public record TransactionDetailResponse(
    DateTime Date,
    DateTime? InvestmentDate,
    decimal Amount,
    string Type,
    decimal? Units,
    decimal? Price,
    decimal? CurrentValue,
    decimal? Returns,
    decimal? Xirr
);

public record PortfolioResponse(
    string Id,
    string Type,
    string ProviderName,
    string? SchemeCode,
    string? TickerSymbol,
    DateTime? SipStartDate,
    int? SipDeductionDay,
    decimal UnitsHeld,
    decimal CurrentValue,
    decimal PurchasePrice,
    decimal Gain,
    decimal GainPercentage,
    decimal? Xirr,
    DateTime LastUpdated
);

// Insurance DTOs
public record InsuranceRequest(
    string Type,
    string PolicyName,
    decimal PremiumAmount,
    DateTime PremiumDueDate,
    decimal SumAssured,
    List<string>? Members
);

public record InsuranceResponse(
    string Id,
    string Type,
    string PolicyName,
    decimal PremiumAmount,
    DateTime PremiumDueDate,
    decimal SumAssured,
    List<string> Members,
    bool IsDueSoon
);

// EPF DTOs
public record EpfSetupRequest(
    string ProviderName,
    decimal OpeningEmployeeBalance,
    decimal OpeningEmployerBalance,
    decimal BasicPay,
    bool IsEpsMember,
    decimal InterestRate,
    int FinancialYear
);

public record UpdateBasicPayRequest(
    decimal NewBasicPay,
    DateTime EffectiveFrom
);

public record EpfContributionResponse(
    DateTime Month,
    decimal EpfWage,
    decimal EmployeeShare,
    decimal EmployerShare,
    decimal EpsWage
);

public record EpfSummaryResponse(
    string Id,
    string ProviderName,
    decimal OpeningEmployeeBalance,
    decimal OpeningEmployerBalance,
    decimal TotalEmployeeContribution,
    decimal TotalEmployerContribution,
    decimal CurrentEmployeeValue,
    decimal CurrentEmployerValue,
    decimal TotalCurrentValue,
    decimal InterestEarned,
    decimal InterestRate,
    bool IsEpsMember,
    decimal CurrentBasicPay,
    List<EpfContributionResponse> Contributions
);

// XIRR DTOs
public record XirrResult(decimal Xirr, decimal AbsoluteReturn, decimal InvestedAmount, decimal CurrentValue);

// SIP Simulation DTOs
public record SipSimulationRequest(decimal MonthlyInvestment, decimal AnnualRate, int Years);
public record SipSimulationPoint(int Month, decimal TotalInvested, decimal FutureValue);
public record SipSimulationResponse(
    decimal TotalInvested,
    decimal FutureValue,
    decimal TotalReturns,
    List<SipSimulationPoint> DataPoints
);

// Stock Price DTOs
public record StockPriceResponse(
    string TickerSymbol,
    decimal CurrentPrice,
    decimal CurrentValue,
    decimal ChangePercent,
    DateTime LastUpdated,
    string? CompanyName,
    string? Sector,
    long? MarketCap
);

public record StockSearchResult(
    string Symbol,
    string ShortName,
    string LongName,
    string Exchange,
    string Type
);

public record MarketMoversResponse(
    List<StockPriceResponse> TopByMarketCap,
    List<StockPriceResponse> TopByReturn
);

// FIRE Calculator DTOs
public record FireCalculationRequest(
    decimal CurrentNetWorth,
    int CurrentAge,
    decimal MonthlyExpenses,
    decimal? TargetAmount,
    int? TargetAge,
    decimal MonthlyInvestment,
    decimal ExpectedAnnualReturn,
    decimal WithdrawalRate
);

public record FireProjection(
    int Year,
    int Age,
    decimal ProjectedWealth,
    decimal AnnualInvestment,
    decimal InvestmentGrowth
);

public record FireCalculationResponse(
    decimal CurrentNetWorth,
    decimal TargetAmount,
    decimal ProgressPercentage,
    int YearsToFire,
    int FireAge,
    decimal MonthlyPassiveIncome,
    decimal AnnualExpenses,
    List<FireProjection> Projections
);

// FIRE Goal DTOs (for goal tracking)
public record FireGoalRequest(
    decimal TargetAmount,
    int TargetYear,
    int? TargetAge,
    decimal ExpectedAnnualReturn,
    decimal ConservativeReturn,
    decimal AggressiveReturn,
    decimal InflationRate,
    decimal WithdrawalRate,
    decimal MonthlyExpenses
);

public record FireGoalResponse(
    string Id,
    decimal TargetAmount,
    int TargetYear,
    int? TargetAge,
    decimal ExpectedAnnualReturn,
    decimal ConservativeReturn,
    decimal AggressiveReturn,
    decimal InflationRate,
    decimal WithdrawalRate,
    decimal MonthlyExpenses,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record FireProgressResponse(
    decimal CurrentNetWorth,
    decimal TargetAmount,
    decimal ProgressPercentage,
    int YearsRemaining,
    decimal RequiredAdditionalSip,
    decimal CurrentMonthlySip,
    decimal TotalMonthlySipNeeded,
    decimal GapAmount,
    int TargetYear,
    int ProjectedFireAge,
    bool OnTrack
);

public record FireScenarioResponse(
    string ScenarioName,
    decimal ReturnRate,
    decimal RequiredSip,
    decimal ProjectedAmount,
    int YearsToGoal
);
