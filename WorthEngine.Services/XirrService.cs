using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services;

/// <summary>
/// XIRR Service using Newton-Raphson method to calculate Internal Rate of Return.
/// Adds a dummy transaction with current value to compute the rate where NPV = 0.
/// </summary>
public class XirrService : IXirrService
{
    private const int MaxIterations = 100;
    private const double Tolerance = 1e-7;
    private const double DefaultGuess = 0.1; // 10% initial guess

    public XirrResult CalculateXirr(List<Transaction> transactions, decimal currentValue)
    {
        if (transactions == null || transactions.Count == 0)
        {
            return new XirrResult(0, 0, 0, currentValue);
        }

        // Prepare cash flows: deposits are negative (money out), withdrawals are positive
        var cashFlows = new List<(DateTime Date, double Amount)>();
        decimal totalInvested = 0;

        foreach (var txn in transactions.OrderBy(t => t.Date))
        {
            // Handle both MF (DEPOSIT/WITHDRAWAL) and Stock (BUY/SELL) transaction types
            double amount = (txn.Type == "DEPOSIT" || txn.Type == "BUY")
                ? -(double)txn.Amount  // Money going out (investment)
                : (double)txn.Amount;   // Money coming in (withdrawal/sell)
            
            cashFlows.Add((txn.Date, amount));
            
            if (txn.Type == "DEPOSIT" || txn.Type == "BUY")
                totalInvested += txn.Amount;
            else if (txn.Type == "WITHDRAWAL" || txn.Type == "SELL")
                totalInvested -= txn.Amount;
        }

        // Add current value as a positive cash flow (as if selling today)
        cashFlows.Add((DateTime.Now, (double)currentValue));

        if (cashFlows.Count < 2)
        {
            return new XirrResult(0, 0, totalInvested, currentValue);
        }

        // Calculate XIRR using Newton-Raphson
        double xirr = CalculateXirrNewtonRaphson(cashFlows);
        
        decimal absoluteReturn = currentValue - totalInvested;
        
        return new XirrResult(
            Xirr: (decimal)Math.Round(xirr * 100, 2), // As percentage
            AbsoluteReturn: absoluteReturn,
            InvestedAmount: totalInvested,
            CurrentValue: currentValue
        );
    }

    private double CalculateXirrNewtonRaphson(List<(DateTime Date, double Amount)> cashFlows)
    {
        var baseDate = cashFlows.Min(cf => cf.Date);
        var normalizedFlows = cashFlows
            .Select(cf => (Years: (cf.Date - baseDate).TotalDays / 365.0, cf.Amount))
            .ToList();

        double rate = DefaultGuess;

        for (int i = 0; i < MaxIterations; i++)
        {
            double npv = 0;
            double derivative = 0;

            foreach (var (years, amount) in normalizedFlows)
            {
                double denominator = Math.Pow(1 + rate, years);
                npv += amount / denominator;
                derivative -= years * amount / Math.Pow(1 + rate, years + 1);
            }

            if (Math.Abs(derivative) < Tolerance)
            {
                break; // Derivative too small, avoid division by zero
            }

            double newRate = rate - npv / derivative;

            if (Math.Abs(newRate - rate) < Tolerance)
            {
                return newRate;
            }

            rate = newRate;

            // Bound the rate to avoid divergence
            if (rate < -0.99) rate = -0.99;
            if (rate > 10) rate = 10; // 1000% max
        }

        return rate;
    }
}
