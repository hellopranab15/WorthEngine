using WorthEngine.Core.DTOs;

namespace WorthEngine.Services;

public class FireCalculatorService
{
    public FireCalculationResponse CalculateFire(FireCalculationRequest request)
    {
        // Calculate target amount if not provided (25x annual expenses)
        decimal annualExpenses = request.MonthlyExpenses * 12;
        decimal targetAmount = request.TargetAmount ?? (annualExpenses * 25);

        // Calculate progress percentage
        decimal progressPercentage = request.CurrentNetWorth / targetAmount * 100;

        // Calculate monthly passive income at FIRE
        decimal monthlyPassiveIncome = (targetAmount * request.WithdrawalRate / 100) / 12;

        // Generate projections
        var projections = GenerateProjections(
            request.CurrentNetWorth,
            request.CurrentAge,
            request.MonthlyInvestment,
            request.ExpectedAnnualReturn,
            targetAmount,
            request.TargetAge
        );

        // Calculate years to FIRE
        int yearsToFire = CalculateYearsToFire(projections, targetAmount);
        int fireAge = request.CurrentAge + yearsToFire;

        return new FireCalculationResponse(
            CurrentNetWorth: request.CurrentNetWorth,
            TargetAmount: targetAmount,
            ProgressPercentage: Math.Round(progressPercentage, 2),
            YearsToFire: yearsToFire,
            FireAge: fireAge,
            MonthlyPassiveIncome: Math.Round(monthlyPassiveIncome, 2),
            AnnualExpenses: Math.Round(annualExpenses, 2),
            Projections: projections
        );
    }

    private List<FireProjection> GenerateProjections(
        decimal currentNetWorth,
        int currentAge,
        decimal monthlyInvestment,
        decimal annualReturnRate,
        decimal targetAmount,
        int? targetAge
    )
    {
        var projections = new List<FireProjection>();
        decimal wealth = currentNetWorth;
        decimal monthlyRate = annualReturnRate / 100 / 12;
        int maxYears = targetAge.HasValue ? (targetAge.Value - currentAge) : 50;

        for (int year = 0; year <= maxYears; year++)
        {
            decimal annualInvestment = monthlyInvestment * 12;
            decimal yearStartWealth = wealth;

            // Calculate compound growth for the year
            for (int month = 0; month < 12; month++)
            {
                wealth += monthlyInvestment;
                wealth *= (1 + monthlyRate);
            }

            decimal investmentGrowth = wealth - yearStartWealth - annualInvestment;

            projections.Add(new FireProjection(
                Year: year,
                Age: currentAge + year,
                ProjectedWealth: Math.Round(wealth, 2),
                AnnualInvestment: Math.Round(annualInvestment, 2),
                InvestmentGrowth: Math.Round(investmentGrowth, 2)
            ));

            // Stop if we've reached the target
            if (wealth >= targetAmount && year > 0)
            {
                break;
            }
        }

        return projections;
    }

    private int CalculateYearsToFire(List<FireProjection> projections, decimal targetAmount)
    {
        var fireProjection = projections.FirstOrDefault(p => p.ProjectedWealth >= targetAmount && p.Year > 0);
        return fireProjection?.Year ?? projections.Last().Year;
    }
}
