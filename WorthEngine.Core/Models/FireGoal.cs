using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorthEngine.Core.Models;

public class FireGoal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("targetAmount")]
    public decimal TargetAmount { get; set; }

    [BsonElement("targetYear")]
    public int TargetYear { get; set; }

    [BsonElement("targetAge")]
    public int? TargetAge { get; set; }

    [BsonElement("expectedAnnualReturn")]
    public decimal ExpectedAnnualReturn { get; set; }

    [BsonElement("conservativeReturn")]
    public decimal ConservativeReturn { get; set; }

    [BsonElement("aggressiveReturn")]
    public decimal AggressiveReturn { get; set; }

    [BsonElement("inflationRate")]
    public decimal InflationRate { get; set; }

    [BsonElement("withdrawalRate")]
    public decimal WithdrawalRate { get; set; } = 4.0m;

    [BsonElement("monthlyExpenses")]
    public decimal MonthlyExpenses { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
