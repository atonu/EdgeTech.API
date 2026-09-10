using EdgeTech.API.Data;
using EdgeTech.API.Models;
using EdgeTech.API.Models.DTOs;
using EdgeTech.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/feedbacks")]
public class FeedbacksController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly IIdGeneratorService _ids;

    public FeedbacksController(MongoDbContext db, IIdGeneratorService ids)
    {
        _db = db;
        _ids = ids;
    }

    /// <summary>
    /// Submit feedback or support message (Public / Anonymous)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { message = "Subject is required." });

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@'))
            return BadRequest(new { message = "If provided, email must be a valid email address." });

        var feedback = new Feedback
        {
            Id = await _ids.NextAsync("feedbacks"),
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Anonymous" : request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Rating = request.Rating is >= 1 and <= 5 ? request.Rating : null,
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };

        await _db.Feedbacks.InsertOneAsync(feedback);

        var dto = new FeedbackDto(
            feedback.Id,
            feedback.Name,
            feedback.Email,
            feedback.Phone,
            feedback.Category,
            feedback.Subject,
            feedback.Message,
            feedback.Rating,
            feedback.Status,
            feedback.AdminNotes,
            feedback.CreatedAt,
            feedback.UpdatedAt
        );

        return CreatedAtAction(nameof(GetById), new { id = feedback.Id }, dto);
    }

    /// <summary>
    /// List all feedbacks with pagination and filters (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var builder = Builders<Feedback>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            filter &= builder.Eq(f => f.Status, status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            filter &= builder.Eq(f => f.Category, category.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var searchFilter = builder.Or(
                builder.Regex(f => f.Name, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                builder.Regex(f => f.Email, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                builder.Regex(f => f.Phone, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                builder.Regex(f => f.Subject, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                builder.Regex(f => f.Message, new MongoDB.Bson.BsonRegularExpression(s, "i"))
            );
            filter &= searchFilter;
        }

        var totalCount = (int)await _db.Feedbacks.CountDocumentsAsync(filter);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await _db.Feedbacks.Find(filter)
            .SortByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var dtos = items.Select(f => new FeedbackDto(
            f.Id,
            f.Name,
            f.Email,
            f.Phone,
            f.Category,
            f.Subject,
            f.Message,
            f.Rating,
            f.Status,
            f.AdminNotes,
            f.CreatedAt,
            f.UpdatedAt
        )).ToList();

        return Ok(new PagedResult<FeedbackDto>(dtos, totalCount, page, pageSize, totalPages));
    }

    /// <summary>
    /// Get feedback details by ID (Admin only)
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var feedback = await _db.Feedbacks.Find(f => f.Id == id).FirstOrDefaultAsync();
        if (feedback == null)
            return NotFound(new { message = $"Feedback #{id} not found." });

        var dto = new FeedbackDto(
            feedback.Id,
            feedback.Name,
            feedback.Email,
            feedback.Phone,
            feedback.Category,
            feedback.Subject,
            feedback.Message,
            feedback.Rating,
            feedback.Status,
            feedback.AdminNotes,
            feedback.CreatedAt,
            feedback.UpdatedAt
        );

        return Ok(dto);
    }

    /// <summary>
    /// Update feedback status and administrative notes (Admin only)
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFeedbackStatusRequest request)
    {
        var feedback = await _db.Feedbacks.Find(f => f.Id == id).FirstOrDefaultAsync();
        if (feedback == null)
            return NotFound(new { message = $"Feedback #{id} not found." });

        var update = Builders<Feedback>.Update
            .Set(f => f.Status, string.IsNullOrWhiteSpace(request.Status) ? feedback.Status : request.Status.Trim())
            .Set(f => f.AdminNotes, request.AdminNotes)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);

        var filter = Builders<Feedback>.Filter.Eq(f => f.Id, id);
        var options = new FindOneAndUpdateOptions<Feedback, Feedback>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _db.Feedbacks.FindOneAndUpdateAsync(filter, update, options);

        var dto = new FeedbackDto(
            updated.Id,
            updated.Name,
            updated.Email,
            updated.Phone,
            updated.Category,
            updated.Subject,
            updated.Message,
            updated.Rating,
            updated.Status,
            updated.AdminNotes,
            updated.CreatedAt,
            updated.UpdatedAt
        );

        return Ok(dto);
    }

    /// <summary>
    /// Delete feedback by ID (Admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await _db.Feedbacks.DeleteOneAsync(f => f.Id == id);
        if (res.DeletedCount == 0)
            return NotFound(new { message = $"Feedback #{id} not found." });

        return Ok(new { message = "Feedback deleted successfully." });
    }
}
