using EdgeTech.API.Data;
using EdgeTech.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/happy-customers")]
public class HappyCustomersController : ControllerBase
{
    private readonly MongoDbContext _db;

    public HappyCustomersController(MongoDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var customers = await _db.HappyCustomers
            .Find(customer => customer.IsActive)
            .SortBy(customer => customer.DisplayOrder)
            .ToListAsync();
        return Ok(customers);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetForAdmin()
    {
        var customers = await _db.HappyCustomers
            .Find(_ => true)
            .SortBy(customer => customer.DisplayOrder)
            .ToListAsync();
        return Ok(customers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] HappyCustomer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.ImageUrl))
            return BadRequest(new { message = "Customer name and image are required." });

        customer.Id = Guid.NewGuid().ToString("N");
        customer.Name = customer.Name.Trim();
        await _db.HappyCustomers.InsertOneAsync(customer);
        return Ok(customer);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] HappyCustomer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.ImageUrl))
            return BadRequest(new { message = "Customer name and image are required." });

        customer.Id = id;
        customer.Name = customer.Name.Trim();
        var result = await _db.HappyCustomers.ReplaceOneAsync(existing => existing.Id == id, customer);
        if (result.MatchedCount == 0)
            return NotFound(new { message = "Customer entry not found." });
        return Ok(customer);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.HappyCustomers.DeleteOneAsync(customer => customer.Id == id);
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Customer entry not found." });
        return NoContent();
    }
}
