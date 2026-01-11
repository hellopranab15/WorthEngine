using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services;

public class InsuranceService : IInsuranceService
{
    private readonly IInsuranceRepository _insuranceRepository;

    public InsuranceService(IInsuranceRepository insuranceRepository)
    {
        _insuranceRepository = insuranceRepository;
    }

    public async Task<IEnumerable<InsuranceResponse>> GetUserInsurancesAsync(string userId)
    {
        var insurances = await _insuranceRepository.GetByUserIdAsync(userId);
        return insurances.Select(MapToResponse);
    }

    public async Task<InsuranceResponse> CreateInsuranceAsync(string userId, InsuranceRequest request)
    {
        var insurance = new Insurance
        {
            UserId = userId,
            Type = request.Type,
            PolicyName = request.PolicyName,
            PremiumAmount = request.PremiumAmount,
            PremiumDueDate = request.PremiumDueDate,
            CoverDetails = new CoverDetails
            {
                SumAssured = request.SumAssured,
                Members = request.Members ?? new List<string>()
            },
            IsActive = true
        };

        await _insuranceRepository.CreateAsync(insurance);
        return MapToResponse(insurance);
    }

    public async Task DeleteInsuranceAsync(string id, string userId)
    {
        var insurance = await _insuranceRepository.GetByIdAsync(id);
        if (insurance != null && insurance.UserId == userId)
        {
            await _insuranceRepository.DeleteAsync(id);
        }
    }

    private static InsuranceResponse MapToResponse(Insurance i) =>
        new InsuranceResponse(
            i.Id,
            i.Type,
            i.PolicyName,
            i.PremiumAmount,
            i.PremiumDueDate,
            i.CoverDetails.SumAssured,
            i.CoverDetails.Members,
            i.IsDueSoon
        );
}
