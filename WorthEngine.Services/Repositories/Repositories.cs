using MongoDB.Bson;
using MongoDB.Driver;
using WorthEngine.Core.Interfaces;
using WorthEngine.Core.Models;

namespace WorthEngine.Services.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<User?> GetByIdAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<User> CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }
}

public class PortfolioRepository : IPortfolioRepository
{
    private readonly IMongoCollection<Portfolio> _portfolios;

    public PortfolioRepository(IMongoDatabase database)
    {
        _portfolios = database.GetCollection<Portfolio>("portfolios");
    }

    public async Task<IEnumerable<Portfolio>> GetByUserIdAsync(string userId) =>
        await _portfolios.Find(p => p.UserId == userId).ToListAsync();

    public async Task<Portfolio?> GetByIdAsync(string id) =>
        await _portfolios.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<Portfolio> CreateAsync(Portfolio portfolio)
    {
        await _portfolios.InsertOneAsync(portfolio);
        return portfolio;
    }

    public async Task UpdateAsync(Portfolio portfolio) =>
        await _portfolios.ReplaceOneAsync(p => p.Id == portfolio.Id, portfolio);

    public async Task DeleteAsync(string id) =>
        await _portfolios.DeleteOneAsync(p => p.Id == id);

    public async Task UpdateCurrentValueAsync(string id, decimal newValue, DateTime lastUpdated)
    {
        var update = Builders<Portfolio>.Update
            .Set(p => p.CurrentValue, newValue)
            .Set(p => p.LastUpdated, lastUpdated);
        await _portfolios.UpdateOneAsync(p => p.Id == id, update);
    }
}

public class InsuranceRepository : IInsuranceRepository
{
    private readonly IMongoCollection<Insurance> _insurances;

    public InsuranceRepository(IMongoDatabase database)
    {
        _insurances = database.GetCollection<Insurance>("insurances");
    }

    public async Task<IEnumerable<Insurance>> GetByUserIdAsync(string userId) =>
        await _insurances.Find(i => i.UserId == userId && i.IsActive).ToListAsync();

    public async Task<Insurance?> GetByIdAsync(string id) =>
        await _insurances.Find(i => i.Id == id).FirstOrDefaultAsync();

    public async Task<Insurance> CreateAsync(Insurance insurance)
    {
        await _insurances.InsertOneAsync(insurance);
        return insurance;
    }

    public async Task UpdateAsync(Insurance insurance) =>
        await _insurances.ReplaceOneAsync(i => i.Id == insurance.Id, insurance);

    public async Task DeleteAsync(string id) =>
        await _insurances.DeleteOneAsync(i => i.Id == id);
}

public class MutualFundRepository : IMutualFundRepository
{
    private readonly IMongoCollection<MutualFundScheme> _schemes;

    public MutualFundRepository(IMongoDatabase database)
    {
        _schemes = database.GetCollection<MutualFundScheme>("mutual_funds");
        
        // Create indexes
        var indexKeys = Builders<MutualFundScheme>.IndexKeys.Text(s => s.SchemeName);
        var indexModel = new CreateIndexModel<MutualFundScheme>(indexKeys);
        _schemes.Indexes.CreateOne(indexModel);
        
        var codeIndex = Builders<MutualFundScheme>.IndexKeys.Ascending(s => s.SchemeCode);
        _schemes.Indexes.CreateOne(new CreateIndexModel<MutualFundScheme>(codeIndex));
    }

    public async Task BulkInsertAsync(IEnumerable<MutualFundScheme> schemes)
    {
        // For simplicity, dropping and recreating. In prod, maybe upsert.
        await _schemes.Database.DropCollectionAsync("mutual_funds");
        // Re-create indexes since we dropped collection
        var indexKeys = Builders<MutualFundScheme>.IndexKeys.Text(s => s.SchemeName);
        _schemes.Indexes.CreateOne(new CreateIndexModel<MutualFundScheme>(indexKeys));
        var codeIndex = Builders<MutualFundScheme>.IndexKeys.Ascending(s => s.SchemeCode);
        _schemes.Indexes.CreateOne(new CreateIndexModel<MutualFundScheme>(codeIndex));

        if (schemes.Any())
        {
            await _schemes.InsertManyAsync(schemes);
        }
    }

    public async Task<IEnumerable<MutualFundScheme>> SearchAsync(string query)
    {
        // Use Regex for partial matching (case-insensitive)
        var filter = Builders<MutualFundScheme>.Filter.Regex(s => s.SchemeName, new BsonRegularExpression(query, "i"));
        return await _schemes.Find(filter)
                             .Limit(50)
                             .ToListAsync();
    }

    public async Task<MutualFundScheme?> GetBySchemeCodeAsync(string schemeCode)
    {
        return await _schemes.Find(s => s.SchemeCode == schemeCode).FirstOrDefaultAsync();
    }
}
