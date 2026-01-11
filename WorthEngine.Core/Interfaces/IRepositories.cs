using WorthEngine.Core.Models;

namespace WorthEngine.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
}

public interface IPortfolioRepository
{
    Task<IEnumerable<Portfolio>> GetByUserIdAsync(string userId);
    Task<Portfolio?> GetByIdAsync(string id);
    Task<Portfolio> CreateAsync(Portfolio portfolio);
    Task UpdateAsync(Portfolio portfolio);
    Task DeleteAsync(string id);
    Task UpdateCurrentValueAsync(string id, decimal newValue, DateTime lastUpdated);
}

public interface IInsuranceRepository
{
    Task<IEnumerable<Insurance>> GetByUserIdAsync(string userId);
    Task<Insurance?> GetByIdAsync(string id);
    Task<Insurance> CreateAsync(Insurance insurance);
    Task UpdateAsync(Insurance insurance);
    Task DeleteAsync(string id);
}

public interface IMutualFundRepository
{
    Task BulkInsertAsync(IEnumerable<MutualFundScheme> schemes);
    Task<IEnumerable<MutualFundScheme>> SearchAsync(string query);
    Task<MutualFundScheme?> GetBySchemeCodeAsync(string schemeCode);
}
