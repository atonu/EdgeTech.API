using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EdgeTech.API.Models;
using Microsoft.Extensions.Options;

namespace EdgeTech.API.Services;

public interface IMetaConversionsService
{
    Task SendPurchaseAsync(Order order, CancellationToken ct = default);
}

public class MetaConversionsService : IMetaConversionsService
{
    private readonly HttpClient _http;
    private readonly MetaOptions _meta;
    private readonly ILogger<MetaConversionsService> _logger;

    public MetaConversionsService(HttpClient http, IOptions<MetaOptions> meta, ILogger<MetaConversionsService> logger)
    {
        _http = http;
        _meta = meta.Value;
        _logger = logger;
    }

    public async Task SendPurchaseAsync(Order order, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_meta.AccessToken) || string.IsNullOrWhiteSpace(_meta.DatasetId))
        {
            _logger.LogWarning("Meta CAPI disabled for order {OrderId}: missing dataset id or access token.", order.Id);
            return;
        }

        if (order.CapiSent)
        {
            return;
        }

        var items = order.Items ?? [];
        var userData = new Dictionary<string, object?>
        {
            ["em"] = Arr(Hash(order.CustomerEmail)),
            ["ph"] = Arr(Hash(NormaliseBdPhone(order.CustomerPhone))),
            ["fn"] = Arr(Hash(FirstName(order.CustomerName))),
            ["ln"] = Arr(Hash(LastName(order.CustomerName))),
            ["ct"] = Arr(Hash(TryCity(order))),
            ["zp"] = Arr(Hash(TryPostalCode(order))),
            ["country"] = Arr(Hash("bd")),
            ["fbp"] = order.Fbp,
            ["fbc"] = order.Fbc,
            ["client_ip_address"] = order.ClientIp,
            ["client_user_agent"] = order.UserAgent,
        }.Where(kvp => kvp.Value is not null)
         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var payload = new
        {
            data = new[]
            {
                new
                {
                    event_name = "Purchase",
                    event_time = new DateTimeOffset(order.CreatedAt).ToUnixTimeSeconds(),
                    event_id = order.Id.ToString(),
                    event_source_url = $"https://www.edgetech.com.bd/order/{order.Id}",
                    action_source = "website",
                    user_data = userData,
                    custom_data = new
                    {
                        currency = "BDT",
                        value = order.TotalAmount,
                        content_type = "product",
                        content_ids = items.Select(i => i.ProductId.ToString()).ToArray(),
                        contents = items.Select(i => new
                        {
                            id = i.ProductId.ToString(),
                            quantity = i.Quantity,
                            item_price = i.UnitPrice,
                        }).ToArray(),
                        num_items = items.Sum(i => i.Quantity),
                        order_id = order.Id.ToString(),
                    }
                }
            },
            test_event_code = string.IsNullOrWhiteSpace(_meta.TestEventCode) ? null : _meta.TestEventCode,
        };

        var url = $"https://graph.facebook.com/{_meta.ApiVersion}/{_meta.DatasetId}/events?access_token={_meta.AccessToken}";
        using var response = await _http.PostAsJsonAsync(url, payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Meta CAPI rejected order {OrderId}: {StatusCode} {Body}", order.Id, response.StatusCode, body);
            return;
        }

        _logger.LogInformation("Meta CAPI accepted order {OrderId}.", order.Id);
    }

    private static string[]? Arr(string? value) => string.IsNullOrWhiteSpace(value) ? null : new[] { value };

    private static string? Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormaliseBdPhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return null;
        if (digits.StartsWith("880")) return digits;
        if (digits.StartsWith("0")) return "88" + digits;
        if (digits.Length == 10 && digits.StartsWith("1")) return "880" + digits;
        return digits;
    }

    private static string? FirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : null;
    }

    private static string? LastName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : null;
    }

    private static string? TryCity(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.ShippingAddress)) return null;
        try
        {
            var shipping = JsonSerializer.Deserialize<Dictionary<string, string>>(order.ShippingAddress);
            return shipping is not null && shipping.TryGetValue("City", out var city) ? city : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryPostalCode(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.ShippingAddress)) return null;
        try
        {
            var shipping = JsonSerializer.Deserialize<Dictionary<string, string>>(order.ShippingAddress);
            return shipping is not null && shipping.TryGetValue("PostalCode", out var postalCode) ? postalCode : null;
        }
        catch
        {
            return null;
        }
    }
}
