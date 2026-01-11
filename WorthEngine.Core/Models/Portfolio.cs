using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorthEngine.Core.Models;

public class EpfContribution
{
    [BsonElement("month")]
    public DateTime Month { get; set; }

    [BsonElement("epfWage")]
    public decimal EpfWage { get; set; }

    [BsonElement("employeeShare")]
    public decimal EmployeeShare { get; set; }

    [BsonElement("employerShare")]
    public decimal EmployerShare { get; set; }

    [BsonElement("epsWage")]
    public decimal EpsWage { get; set; }
}

public class Transaction
{
    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "DEPOSIT"; // DEPOSIT or WITHDRAWAL

    [BsonElement("investmentDate")]
    public DateTime? InvestmentDate { get; set; } // Date when investment was actually made (for lumpsum tracking)

    [BsonElement("units")]
    public decimal? Units { get; set; } // Number of units/shares purchased in this transaction

    [BsonElement("price")]
    public decimal? Price { get; set; } // NAV/Price per unit at the time of transaction
}

public class Portfolio
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("type")]
    public string Type { get; set; } = null!; // SIP, EPF, NPS, SAVING, STOCK

    [BsonElement("providerName")]
    public string ProviderName { get; set; } = null!;

    [BsonElement("schemeCode")]
    public string? SchemeCode { get; set; } // For MFAPI (e.g., "12345")

    [BsonElement("tickerSymbol")]
    public string? TickerSymbol { get; set; } // For Yahoo Finance (e.g., "TCS.NS")

    [BsonElement("sipStartDate")]
    public DateTime? SipStartDate { get; set; }

    [BsonElement("sipDeductionDay")]
    public int? SipDeductionDay { get; set; }

    [BsonElement("unitsHeld")]
    public decimal UnitsHeld { get; set; }

    [BsonElement("currentValue")]
    public decimal CurrentValue { get; set; }

    [BsonElement("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [BsonElement("transactions")]
    public List<Transaction> Transactions { get; set; } = new();

    // EPF specific properties
    [BsonElement("openingEmployeeBalance")]
    public decimal? OpeningEmployeeBalance { get; set; }

    [BsonElement("openingEmployerBalance")]
    public decimal? OpeningEmployerBalance { get; set; }

    [BsonElement("epfBasicPay")]
    public decimal? EpfBasicPay { get; set; }

    [BsonElement("isEpsMember")]
    public bool? IsEpsMember { get; set; }

    [BsonElement("epfInterestRate")]
    public decimal? EpfInterestRate { get; set; }

    [BsonElement("epfContributions")]
    public List<EpfContribution>? EpfContributions { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
