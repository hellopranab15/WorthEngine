using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorthEngine.Core.Models;

public class CoverDetails
{
    [BsonElement("sumAssured")]
    public decimal SumAssured { get; set; }

    [BsonElement("members")]
    public List<string> Members { get; set; } = new();
}

public class Insurance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("type")]
    public string Type { get; set; } = null!; // TERM or HEALTH

    [BsonElement("policyName")]
    public string PolicyName { get; set; } = null!;

    [BsonElement("premiumAmount")]
    public decimal PremiumAmount { get; set; }

    [BsonElement("premiumDueDate")]
    public DateTime PremiumDueDate { get; set; }

    [BsonElement("coverDetails")]
    public CoverDetails CoverDetails { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonIgnore]
    public bool IsDueSoon => (PremiumDueDate - DateTime.UtcNow).TotalDays < 30;
}
