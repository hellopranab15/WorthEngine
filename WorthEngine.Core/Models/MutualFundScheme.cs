using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorthEngine.Core.Models;

public class MutualFundScheme
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("schemeCode")]
    public string SchemeCode { get; set; } = null!;

    [BsonElement("isinDiv")]
    public string? IsinDiv { get; set; }

    [BsonElement("isinGrowth")]
    public string? IsinGrowth { get; set; }

    [BsonElement("schemeName")]
    public string SchemeName { get; set; } = null!;

    [BsonElement("netAssetValue")]
    public decimal NetAssetValue { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }

    // Text search score for sorting
    [BsonIgnore]
    public double? TextMatchScore { get; set; }
}
