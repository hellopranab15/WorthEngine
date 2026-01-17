using WorthEngine.Core.Models;

namespace WorthEngine.Core.Interfaces;

public interface IFireGoalRepository
{
    Task<FireGoal?> GetByUserIdAsync(string userId);
    Task<FireGoal> SaveAsync(FireGoal fireGoal);
    Task<FireGoal> UpdateAsync(FireGoal fireGoal);
    Task<bool> DeleteAsync(string id);
}
