using System.Text.Json;
using MongoDB.Bson.Serialization.Attributes;

namespace EdgeTech.API.Models;

[BsonIgnoreExtraElements]
public class Order
{
    public int Id { get; set; }
    public string? OrderNumber { get; set; }
    public string? UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public bool IsGuestOrder { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty; // JSON
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public bool IsEmi { get; set; }
    public int? EmiTenureMonths { get; set; }
    public int EmiCompletedMonths { get; set; }
    public decimal? EmiMonthlyAmount { get; set; }
    public string? EmiBank { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];

    // Bundle lines. Component products are decremented from stock like standalone items, but they
    // are billed as a single unit at PackagePrice — recorded here so admins see the applied discount.
    public List<OrderPackage> Packages { get; set; } = [];
}

// A package line captured on an order at the price it was sold for.
public class OrderPackage
{
    public int PackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RegularPrice { get; set; }
    public decimal PackagePrice { get; set; }
    public int Quantity { get; set; } = 1;
    public List<OrderPackageItem> Items { get; set; } = [];
}

public class OrderPackageItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum OrderStatus
{
    Placed,
    Verified,
    InProgress,
    Done,
    Cancelled
}
