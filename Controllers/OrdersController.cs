using System.Security.Claims;
using System.Text.Json;
using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public OrdersController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
    {
        var userId = UserId;

        var packageRequests = (req.Packages ?? [])
            .Where(p => p.Quantity > 0)
            .ToList();

        List<CartItem> cartItems;
        if (req.Items != null && req.Items.Count > 0)
        {
            cartItems = req.Items
                .Where(i => i.Quantity > 0)
                .Select(i => new CartItem { ProductId = i.ProductId, Quantity = i.Quantity, UserId = userId ?? "guest" })
                .ToList();
        }
        else if (userId != null && packageRequests.Count == 0)
        {
            // Only fall back to the persisted server cart when the client sent neither items nor packages.
            cartItems = await _db.CartItems.Find(ci => ci.UserId == userId).ToListAsync();
        }
        else
        {
            cartItems = [];
        }

        if (!cartItems.Any() && packageRequests.Count == 0)
            return BadRequest(new { message = "Cart is empty" });

        // Resolve and validate the selected bundle packages.
        var packageIds = packageRequests.Select(p => p.PackageId).Distinct().ToList();
        var packages = await _db.Packages.Find(p => packageIds.Contains(p.Id)).ToListAsync();
        var packageMap = packages.ToDictionary(p => p.Id);

        foreach (var pr in packageRequests)
        {
            if (!packageMap.TryGetValue(pr.PackageId, out var pkg) || !pkg.IsActive)
                return BadRequest(new { message = $"Package #{pr.PackageId} is no longer available" });
        }

        // Aggregate the required quantity for every product across standalone items and package components,
        // so stock is validated (and decremented) once per product even when it appears in several places.
        var requiredQuantities = new Dictionary<int, int>();
        foreach (var ci in cartItems)
            requiredQuantities[ci.ProductId] = requiredQuantities.GetValueOrDefault(ci.ProductId) + ci.Quantity;
        foreach (var pr in packageRequests)
            foreach (var item in packageMap[pr.PackageId].Items)
                requiredQuantities[item.ProductId] = requiredQuantities.GetValueOrDefault(item.ProductId) + item.Quantity * pr.Quantity;

        var productIds = requiredQuantities.Keys.ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        var outOfStock = requiredQuantities
            .Where(kv => !productMap.ContainsKey(kv.Key)
                         || !productMap[kv.Key].IsActive
                         || kv.Value > productMap[kv.Key].Stock)
            .Select(kv => new
            {
                ProductId = kv.Key,
                Name = productMap.ContainsKey(kv.Key) ? productMap[kv.Key].Name : "Unknown",
                requested = kv.Value,
                available = productMap.ContainsKey(kv.Key) ? productMap[kv.Key].Stock : 0,
                isActive = productMap.ContainsKey(kv.Key) && productMap[kv.Key].IsActive
            })
            .ToList();

        if (outOfStock.Any())
            return BadRequest(new { message = "Some items are out of stock or inactive", items = outOfStock });

        var orderId = await _ids.NextAsync("orders");

        var orderNumber = $"ET-{orderId:D6}";
        var standaloneTotal = cartItems.Sum(ci => (productMap[ci.ProductId].DiscountPrice ?? productMap[ci.ProductId].Price) * ci.Quantity);
        var packagesTotal = packageRequests.Sum(pr => packageMap[pr.PackageId].PackagePrice * pr.Quantity);
        var totalAmount = standaloneTotal + packagesTotal;
        var isEmi = req.IsEmi || (req.PaymentMethod != null && req.PaymentMethod.Equals("emi", StringComparison.OrdinalIgnoreCase));
        var tenureMonths = req.EmiTenureMonths ?? (isEmi ? 12 : null);
        var monthlyAmount = isEmi && tenureMonths.HasValue && tenureMonths.Value > 0
            ? Math.Round(totalAmount / tenureMonths.Value, 2)
            : (decimal?)null;

        var order = new Order
        {
            Id = orderId,
            OrderNumber = orderNumber,
            UserId = userId,
            Status = OrderStatus.Placed,
            CustomerName = req.Customer.FullName,
            CustomerEmail = req.Customer.Email,
            CustomerPhone = req.Customer.Phone,
            IsGuestOrder = string.IsNullOrWhiteSpace(userId),
            ShippingAddress = JsonSerializer.Serialize(req.ShippingAddress),
            Notes = req.Notes,
            AdminNotes = null,
            PaymentMethod = req.PaymentMethod,
            IsEmi = isEmi,
            EmiTenureMonths = tenureMonths,
            EmiCompletedMonths = 0,
            EmiMonthlyAmount = monthlyAmount,
            EmiBank = req.EmiBank,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [],
            Packages = []
        };

        foreach (var ci in cartItems)
        {
            var product = productMap[ci.ProductId];
            order.Items.Add(new OrderItem
            {
                Id = await _ids.NextAsync("orderItems"),
                OrderId = orderId,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = product.DiscountPrice ?? product.Price,
                ProductSnapshot = JsonSerializer.Serialize(new { product.Name, product.SKU })
            });
        }

        foreach (var pr in packageRequests)
        {
            var pkg = packageMap[pr.PackageId];
            order.Packages.Add(new OrderPackage
            {
                PackageId = pkg.Id,
                Name = pkg.Name,
                RegularPrice = pkg.RegularPrice,
                PackagePrice = pkg.PackagePrice,
                Quantity = pr.Quantity,
                Items = pkg.Items.Select(item => new OrderPackageItem
                {
                    ProductId = item.ProductId,
                    ProductName = productMap.GetValueOrDefault(item.ProductId)?.Name ?? "Unknown Product",
                    Quantity = item.Quantity,
                    UnitPrice = productMap.TryGetValue(item.ProductId, out var p) ? (p.DiscountPrice ?? p.Price) : 0m
                }).ToList()
            });
        }

        // Decrement stock once per product using the aggregated totals.
        foreach (var (productId, quantity) in requiredQuantities)
        {
            await _db.Products.UpdateOneAsync(p => p.Id == productId,
                Builders<Product>.Update
                    .Inc(p => p.Stock, -quantity)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow));
        }

        await _db.Orders.InsertOneAsync(order);
        if (userId != null)
            await _db.CartItems.DeleteManyAsync(ci => ci.UserId == userId);

        return Ok(new { orderId = order.Id, orderNumber = order.OrderNumber });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        if (UserId == null) return Unauthorized();

        var orders = await _db.Orders.Find(o => o.UserId == UserId)
            .SortByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(await MapOrdersToDtos(orders));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var order = await _db.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null) return NotFound();
        if (!isAdmin && order.UserId != userId) return NotFound();

        var dtos = await MapOrdersToDtos([order]);
        return Ok(dtos.First());
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<Order>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var textFilter = Builders<Order>.Filter.Or(
                Builders<Order>.Filter.Regex(o => o.CustomerName, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.CustomerEmail, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.CustomerPhone, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.OrderNumber, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.ShippingAddress, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.Notes, new MongoDB.Bson.BsonRegularExpression(term, "i")),
                Builders<Order>.Filter.Regex(o => o.AdminNotes, new MongoDB.Bson.BsonRegularExpression(term, "i"))
            );
            filter = int.TryParse(term, out var orderId)
                ? Builders<Order>.Filter.Or(textFilter, Builders<Order>.Filter.Eq(o => o.Id, orderId))
                : textFilter;
        }

        var total = (int)await _db.Orders.CountDocumentsAsync(filter);
        var orders = await _db.Orders.Find(filter)
            .SortByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var dtos = await MapOrdersToDtos(orders);
        return Ok(new PagedResult<OrderDto>(dtos, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize)));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var update = Builders<Order>.Update
            .Set(o => o.Status, req.Status)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);

        var result = await _db.Orders.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Order, Order>
        {
            ReturnDocument = ReturnDocument.After
        });

        if (result == null) return NotFound();
        return Ok(new { status = result.Status.ToString() });
    }

    [HttpPut("{id}/admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderAdmin(int id, [FromBody] UpdateOrderAdminRequest req)
    {
        var adminNotes = req.AdminNotes ?? req.Notes;
        var update = Builders<Order>.Update
            .Set(o => o.Status, req.Status)
            .Set(o => o.AdminNotes, adminNotes)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        if (req.Notes != null)
        {
            update = update.Set(o => o.Notes, req.Notes);
        }

        if (req.EmiCompletedMonths.HasValue)
        {
            update = update.Set(o => o.EmiCompletedMonths, req.EmiCompletedMonths.Value);
        }

        if (req.EmiTenureMonths.HasValue)
        {
            update = update.Set(o => o.EmiTenureMonths, req.EmiTenureMonths.Value);
        }

        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);

        var result = await _db.Orders.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Order, Order>
        {
            ReturnDocument = ReturnDocument.After
        });

        if (result == null) return NotFound();
        return Ok(new {
            status = result.Status.ToString(),
            notes = result.Notes,
            adminNotes = result.AdminNotes,
            isEmi = result.IsEmi,
            emiCompletedMonths = result.EmiCompletedMonths,
            emiTenureMonths = result.EmiTenureMonths,
            emiMonthlyAmount = result.EmiMonthlyAmount
        });
    }

    private async Task<List<OrderDto>> MapOrdersToDtos(IEnumerable<Order> orders)
    {
        var orderList = orders.ToList();
        var productIds = orderList
            .SelectMany(o => o.Items.Select(i => i.ProductId)
                .Concat(o.Packages.SelectMany(pkg => pkg.Items.Select(pi => pi.ProductId))))
            .Distinct()
            .ToList();
        var products = await _db.Products.Find(p => productIds.Contains(p.Id)).ToListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        string? ImageFor(int productId) =>
            productMap.GetValueOrDefault(productId)?.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                ?? productMap.GetValueOrDefault(productId)?.Images.FirstOrDefault()?.ImageUrl;

        return orderList.Select(o =>
        {
            ShippingAddressDto address;
            try
            {
                address = JsonSerializer.Deserialize<ShippingAddressDto>(o.ShippingAddress) ??
                    new ShippingAddressDto("", "", "", "", "", "", "");
            }
            catch
            {
                address = new ShippingAddressDto("", "", "", "", "", "", "");
            }

            var orderNumber = !string.IsNullOrWhiteSpace(o.OrderNumber) ? o.OrderNumber : $"ET-{o.Id:D6}";

            return new OrderDto(
                o.Id,
                orderNumber,
                o.Status,
                o.TotalAmount,
                new CustomerInfoDto(o.CustomerName, o.CustomerEmail, o.CustomerPhone),
                address,
                o.Notes,
                o.AdminNotes,
                o.PaymentMethod,
                o.CreatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    productMap.GetValueOrDefault(i.ProductId)?.Name ?? "Unknown Product",
                    ImageFor(i.ProductId),
                    i.UnitPrice,
                    i.Quantity
                )).ToList(),
                o.IsEmi,
                o.EmiTenureMonths,
                o.EmiCompletedMonths,
                o.EmiMonthlyAmount,
                o.EmiBank,
                o.Packages.Select(pkg => new OrderPackageDto(
                    pkg.PackageId,
                    pkg.Name,
                    pkg.RegularPrice,
                    pkg.PackagePrice,
                    pkg.Quantity,
                    pkg.Items.Select(pi => new OrderPackageItemDto(
                        pi.ProductId,
                        pi.ProductName,
                        ImageFor(pi.ProductId),
                        pi.Quantity,
                        pi.UnitPrice
                    )).ToList()
                )).ToList()
            );
        }).ToList();
    }
}
