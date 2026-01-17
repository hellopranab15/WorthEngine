using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services;

public class FireGoalService
{
    private readonly IFireGoalRepository _fireGoalRepository;
    private readonly IPortfolioRepository _portfolioRepository;

    public FireGoalService(
        IFireGoalRepository fireGoalRepository,
        IPortfolioRepository portfolioRepository)
    {
        _fireGoalRepository = fireGoalRepository;
        _portfolioRepository = portfolioRepository;
    }

    public async Task<FireGoalResponse> SaveGoalAsync(string userId, FireGoalRequest request)
    {
        // Check if user already has a goal
        var existingGoal = await _fireGoalRepository.GetByUserIdAsync(userId);

        if (existingGoal != null)
        {
            // Update existing goal
            existingGoal.TargetAmount = request.TargetAmount;
            existingGoal.TargetYear = request.TargetYear;
            existingGoal.TargetAge = request.TargetAge;
            existingGoal.ExpectedAnnualReturn = request.ExpectedAnnualReturn;
            existingGoal.ConservativeReturn = request.ConservativeReturn;
            existingGoal.AggressiveReturn = request.AggressiveReturn;
            existingGoal.InflationRate = request.InflationRate;
            existingGoal.WithdrawalRate = request.WithdrawalRate;
            existingGoal.MonthlyExpenses = request.MonthlyExpenses;

            var updated = await _fireGoalRepository.UpdateAsync(existingGoal);
            return MapToResponse(updated);
        }
        else
        {
            // Create new goal
            var fireGoal = new FireGoal
            {
                UserId = userId,
                TargetAmount = request.TargetAmount,
                TargetYear = request.TargetYear,
                TargetAge = request.TargetAge,
                ExpectedAnnualReturn = request.ExpectedAnnualReturn,
                ConservativeReturn = request.ConservativeReturn,
                AggressiveReturn = request.AggressiveReturn,
                InflationRate = request.InflationRate,
                WithdrawalRate = request.WithdrawalRate,
                MonthlyExpenses = request.MonthlyExpenses
            };

            var saved = await _fireGoalRepository.SaveAsync(fireGoal);
            return MapToResponse(saved);
        }
    }

    public async Task<FireGoalResponse?> GetGoalAsync(string userId)
    {
        var goal = await _fireGoalRepository.GetByUserIdAsync(userId);
        return goal != null ? MapToResponse(goal) : null;
    }

    public async Task<FireProgressResponse?> GetProgressAsync(string userId)
    {
        var goal = await _fireGoalRepository.GetByUserIdAsync(userId);
        if (goal == null) return null;

        // Get current net worth from portfolio
        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
        var currentNetWorth = portfolios.Sum(p => p.CurrentValue);

        // Calculate current monthly SIP
        var currentMonthlySip = await GetCurrentMonthlySipAsync(userId);

        // Calculate years remaining
        int currentYear = DateTime.UtcNow.Year;
        int yearsRemaining = goal.TargetYear - currentYear;
        int monthsRemaining = yearsRemaining * 12;

        // Calculate progress percentage
        decimal progressPercentage = currentNetWorth / goal.TargetAmount * 100;

        // Calculate gap amount
        decimal gapAmount = goal.TargetAmount - currentNetWorth;

        // Calculate required total SIP
        decimal totalSipNeeded = CalculateRequiredSip(
            currentNetWorth,
            goal.TargetAmount,
            monthsRemaining > 0 ? monthsRemaining : 1,
            goal.ExpectedAnnualReturn
        );

        // Calculate additional SIP needed
        decimal requiredAdditionalSip = Math.Max(0, totalSipNeeded - currentMonthlySip);

        // Calculate projected FIRE age
        int currentAge = DateTime.UtcNow.Year - (DateTime.UtcNow.Year - 30); // Approximate, could be improved
        int projectedFireAge = currentAge + yearsRemaining;

        // Determine if on track
        bool onTrack = progressPercentage >= ((decimal)(currentYear - (goal.TargetYear - yearsRemaining)) / yearsRemaining * 100);

        return new FireProgressResponse(
            CurrentNetWorth: currentNetWorth,
            TargetAmount: goal.TargetAmount,
            ProgressPercentage: Math.Round(progressPercentage, 2),
            YearsRemaining: yearsRemaining,
            RequiredAdditionalSip: Math.Round(requiredAdditionalSip, 2),
            CurrentMonthlySip: Math.Round(currentMonthlySip, 2),
            TotalMonthlySipNeeded: Math.Round(totalSipNeeded, 2),
            GapAmount: Math.Round(gapAmount, 2),
            TargetYear: goal.TargetYear,
            ProjectedFireAge: projectedFireAge,
            OnTrack: onTrack
        );
    }

    public async Task<List<FireScenarioResponse>> GetScenariosAsync(string userId)
    {
        var goal = await _fireGoalRepository.GetByUserIdAsync(userId);
        if (goal == null) return new List<FireScenarioResponse>();

        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
        var currentNetWorth = portfolios.Sum(p => p.CurrentValue);

        int currentYear = DateTime.UtcNow.Year;
        int yearsRemaining = goal.TargetYear - currentYear;
        int monthsRemaining = yearsRemaining * 12;

        var scenarios = new List<FireScenarioResponse>();

        // Conservative scenario
        var conservativeSip = CalculateRequiredSip(currentNetWorth, goal.TargetAmount, monthsRemaining, goal.ConservativeReturn);
        scenarios.Add(new FireScenarioResponse(
            ScenarioName: "Conservative",
            ReturnRate: goal.ConservativeReturn,
            RequiredSip: Math.Round(conservativeSip, 2),
            ProjectedAmount: CalculateProjectedAmount(currentNetWorth, conservativeSip, monthsRemaining, goal.ConservativeReturn),
            YearsToGoal: yearsRemaining
        ));

        // Moderate scenario
        var moderateSip = CalculateRequiredSip(currentNetWorth, goal.TargetAmount, monthsRemaining, goal.ExpectedAnnualReturn);
        scenarios.Add(new FireScenarioResponse(
            ScenarioName: "Moderate",
            ReturnRate: goal.ExpectedAnnualReturn,
            RequiredSip: Math.Round(moderateSip, 2),
            ProjectedAmount: CalculateProjectedAmount(currentNetWorth, moderateSip, monthsRemaining, goal.ExpectedAnnualReturn),
            YearsToGoal: yearsRemaining
        ));

        // Aggressive scenario
        var aggressiveSip = CalculateRequiredSip(currentNetWorth, goal.TargetAmount, monthsRemaining, goal.AggressiveReturn);
        scenarios.Add(new FireScenarioResponse(
            ScenarioName: "Aggressive",
            ReturnRate: goal.AggressiveReturn,
            RequiredSip: Math.Round(aggressiveSip, 2),
            ProjectedAmount: CalculateProjectedAmount(currentNetWorth, aggressiveSip, monthsRemaining, goal.AggressiveReturn),
            YearsToGoal: yearsRemaining
        ));

        return scenarios;
    }

    private async Task<decimal> GetCurrentMonthlySipAsync(string userId)
    {
        var portfolios = await _portfolioRepository.GetByUserIdAsync(userId);
        
        decimal totalMonthlySip = 0;

        foreach (var portfolio in portfolios.Where(p => p.Type == "SIP"))
        {
            // Calculate average monthly investment from transactions
            if (portfolio.Transactions != null && portfolio.Transactions.Any())
            {
                var sipTransactions = portfolio.Transactions.Where(t => t.Type == "DEPOSIT").ToList();
                if (sipTransactions.Any())
                {
                    // Simple average of transaction amounts
                    totalMonthlySip += sipTransactions.Average(t => t.Amount);
                }
            }
        }

        return totalMonthlySip;
    }

    private decimal CalculateRequiredSip(decimal presentValue, decimal futureValue, int months, decimal annualReturnRate)
    {
        if (months <= 0) return 0;

        decimal monthlyRate = annualReturnRate / 100 / 12;
        
        if (monthlyRate == 0)
        {
            // If no interest, simple division
            return (futureValue - presentValue) / months;
        }

        // Future Value of lump sum
        decimal futureValueOfPresentAmount = presentValue * (decimal)Math.Pow((double)(1 + monthlyRate), months);
        
        // Remaining amount to be accumulated through SIP
        decimal remainingAmount = futureValue - futureValueOfPresentAmount;
        
        if (remainingAmount <= 0) return 0;

        // SIP formula: FV = SIP * (((1+r)^n - 1) / r) * (1+r)
        decimal sipMultiplier = (decimal)(Math.Pow((double)(1 + monthlyRate), months) - 1) / monthlyRate;
        
        return remainingAmount / sipMultiplier;
    }

    private decimal CalculateProjectedAmount(decimal presentValue, decimal monthlySip, int months, decimal annualReturnRate)
    {
        decimal monthlyRate = annualReturnRate / 100 / 12;
        
        // Future value of present amount
        decimal fvPresent = presentValue * (decimal)Math.Pow((double)(1 + monthlyRate), months);
        
        // Future value of SIP
        decimal fvSip = 0;
        if (monthlyRate != 0)
        {
            fvSip = monthlySip * (decimal)(Math.Pow((double)(1 + monthlyRate), months) - 1) / monthlyRate;
        }
        else
        {
            fvSip = monthlySip * months;
        }
        
        return Math.Round(fvPresent + fvSip, 2);
    }

    private FireGoalResponse MapToResponse(FireGoal goal)
    {
        return new FireGoalResponse(
            Id: goal.Id,
            TargetAmount: goal.TargetAmount,
            TargetYear: goal.TargetYear,
            TargetAge: goal.TargetAge,
            ExpectedAnnualReturn: goal.ExpectedAnnualReturn,
            ConservativeReturn: goal.ConservativeReturn,
            AggressiveReturn: goal.AggressiveReturn,
            InflationRate: goal.InflationRate,
            WithdrawalRate: goal.WithdrawalRate,
            MonthlyExpenses: goal.MonthlyExpenses,
            IsActive: goal.IsActive,
            CreatedAt: goal.CreatedAt,
            UpdatedAt: goal.UpdatedAt
        );
    }
}
