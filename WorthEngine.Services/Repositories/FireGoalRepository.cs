using MongoDB.Driver;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services.Repositories;

public class FireGoalRepository : IFireGoalRepository
{
    private readonly IMongoCollection<FireGoal> _fireGoals;

    public FireGoalRepository(IMongoDatabase database)
    {
        _fireGoals = database.GetCollection<FireGoal>("firegoals");
    }

    public async Task<FireGoal?> GetByUserIdAsync(string userId)
    {
        return await _fireGoals
            .Find(fg => fg.UserId == userId && fg.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<FireGoal> SaveAsync(FireGoal fireGoal)
    {
        fireGoal.CreatedAt = DateTime.UtcNow;
        fireGoal.UpdatedAt = DateTime.UtcNow;
        fireGoal.IsActive = true;
        
        await _fireGoals.InsertOneAsync(fireGoal);
        return fireGoal;
    }

    public async Task<FireGoal> UpdateAsync(FireGoal fireGoal)
    {
        fireGoal.UpdatedAt = DateTime.UtcNow;
        
        await _fireGoals.ReplaceOneAsync(
            fg => fg.Id == fireGoal.Id,
            fireGoal
        );
        
        return fireGoal;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _fireGoals.DeleteOneAsync(fg => fg.Id == id);
        return result.DeletedCount > 0;
    }
}
