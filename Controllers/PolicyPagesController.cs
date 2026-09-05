using EdgeTech.API.Data;
using EdgeTech.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/policy-pages")]
public class PolicyPagesController : ControllerBase
{
    private readonly MongoDbContext _db;

    public PolicyPagesController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pages = await _db.PolicyPages.Find(_ => true)
            .Project(p => new
            {
                p.Id,
                p.Slug,
                p.Title,
                p.Subtitle,
                p.Badge,
                p.LastUpdated,
                p.UpdatedAt,
                SectionsCount = p.Sections.Count
            })
            .ToListAsync();

        return Ok(pages);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await _db.PolicyPages.Find(p => p.Slug == normalizedSlug).FirstOrDefaultAsync();

        if (page == null)
        {
            // Fallback to seed defaults if missing
            var defaults = PolicyPagesSeed.GetDefaultPages();
            var matched = defaults.FirstOrDefault(d => d.Slug == normalizedSlug);
            if (matched != null)
            {
                matched.UpdatedAt = DateTime.UtcNow;
                await _db.PolicyPages.InsertOneAsync(matched);
                return Ok(matched);
            }
            return NotFound(new { message = $"Policy page '{slug}' not found" });
        }

        return Ok(page);
    }

    [HttpPut("{slug}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string slug, [FromBody] PolicyPage request)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var existing = await _db.PolicyPages.Find(p => p.Slug == normalizedSlug).FirstOrDefaultAsync();

        if (existing == null)
        {
            request.Slug = normalizedSlug;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.PolicyPages.InsertOneAsync(request);
            return Ok(request);
        }

        existing.Title = request.Title;
        existing.Subtitle = request.Subtitle;
        existing.Badge = request.Badge;
        existing.LastUpdated = request.LastUpdated;
        existing.Sections = request.Sections ?? new List<PolicySection>();
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.PolicyPages.ReplaceOneAsync(p => p.Slug == normalizedSlug, existing);
        return Ok(existing);
    }

    public record AddSectionDto(string Title, string Body, string? HighlightTitle = null, string? HighlightText = null);

    [HttpPost("{slug}/sections")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSection(string slug, [FromBody] AddSectionDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Section title is required" });

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await _db.PolicyPages.Find(p => p.Slug == normalizedSlug).FirstOrDefaultAsync();

        if (page == null)
        {
            var defaults = PolicyPagesSeed.GetDefaultPages();
            page = defaults.FirstOrDefault(d => d.Slug == normalizedSlug);
            if (page == null)
            {
                page = new PolicyPage
                {
                    Slug = normalizedSlug,
                    Title = char.ToUpperInvariant(normalizedSlug[0]) + normalizedSlug[1..],
                    Sections = new List<PolicySection>()
                };
            }
            await _db.PolicyPages.InsertOneAsync(page);
        }

        var nextOrder = (page.Sections != null && page.Sections.Count > 0)
            ? page.Sections.Max(s => s.Order) + 1
            : 1;

        var newSection = new PolicySection
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title.Trim(),
            Body = request.Body?.Trim() ?? string.Empty,
            HighlightTitle = string.IsNullOrWhiteSpace(request.HighlightTitle) ? null : request.HighlightTitle.Trim(),
            HighlightText = string.IsNullOrWhiteSpace(request.HighlightText) ? null : request.HighlightText.Trim(),
            Order = nextOrder
        };

        page.Sections ??= new List<PolicySection>();
        page.Sections.Add(newSection);
        page.UpdatedAt = DateTime.UtcNow;

        await _db.PolicyPages.ReplaceOneAsync(p => p.Slug == normalizedSlug, page);
        return Ok(page);
    }

    [HttpDelete("{slug}/sections/{sectionId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSection(string slug, string sectionId)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await _db.PolicyPages.Find(p => p.Slug == normalizedSlug).FirstOrDefaultAsync();

        if (page == null)
            return NotFound(new { message = $"Policy page '{slug}' not found" });

        var countBefore = page.Sections?.Count ?? 0;
        page.Sections = page.Sections?.Where(s => s.Id != sectionId).ToList() ?? new List<PolicySection>();

        if (page.Sections.Count == countBefore)
            return NotFound(new { message = $"Section '{sectionId}' not found" });

        page.UpdatedAt = DateTime.UtcNow;
        await _db.PolicyPages.ReplaceOneAsync(p => p.Slug == normalizedSlug, page);

        return Ok(page);
    }

    public record UpdateFieldDto(string Path, string Value);

    [HttpPatch("{slug}/field")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateField(string slug, [FromBody] UpdateFieldDto request)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await _db.PolicyPages.Find(p => p.Slug == normalizedSlug).FirstOrDefaultAsync();

        if (page == null)
            return NotFound(new { message = $"Policy page '{slug}' not found" });

        var parts = request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            switch (parts[0].ToLowerInvariant())
            {
                case "title": page.Title = request.Value; break;
                case "subtitle": page.Subtitle = request.Value; break;
                case "badge": page.Badge = request.Value; break;
                case "lastupdated": page.LastUpdated = request.Value; break;
                default: return BadRequest(new { message = $"Unknown root field '{parts[0]}'" });
            }
        }
        else if (parts.Length >= 3 && parts[0].Equals("sections", StringComparison.OrdinalIgnoreCase))
        {
            var sectionId = parts[1];
            var section = page.Sections?.FirstOrDefault(s => s.Id == sectionId);
            if (section == null) return NotFound(new { message = $"Section '{sectionId}' not found" });

            var field = parts[2].ToLowerInvariant();
            if (parts.Length == 3)
            {
                switch (field)
                {
                    case "title": section.Title = request.Value; break;
                    case "body": section.Body = request.Value; break;
                    case "highlighttitle": section.HighlightTitle = request.Value; break;
                    case "highlighttext": section.HighlightText = request.Value; break;
                    default: return BadRequest(new { message = $"Unknown section field '{field}'" });
                }
            }
            else if (parts.Length == 5 && field == "subitems")
            {
                var subItemId = parts[3];
                var subField = parts[4].ToLowerInvariant();
                var subItem = section.SubItems?.FirstOrDefault(si => si.Id == subItemId);
                if (subItem == null) return NotFound(new { message = $"SubItem '{subItemId}' not found" });

                switch (subField)
                {
                    case "title": subItem.Title = request.Value; break;
                    case "text": subItem.Text = request.Value; break;
                    case "subtitle": subItem.Subtitle = request.Value; break;
                    case "tag": subItem.Tag = request.Value; break;
                    default: return BadRequest(new { message = $"Unknown subitem field '{subField}'" });
                }
            }
            else if (parts.Length == 4 && field == "listitems")
            {
                if (int.TryParse(parts[3], out var index) && section.ListItems != null && index >= 0 && index < section.ListItems.Count)
                {
                    section.ListItems[index] = request.Value;
                }
                else
                {
                    return BadRequest(new { message = "Invalid list item index" });
                }
            }
        }
        else
        {
            return BadRequest(new { message = "Invalid field path" });
        }

        page.UpdatedAt = DateTime.UtcNow;
        await _db.PolicyPages.ReplaceOneAsync(p => p.Slug == normalizedSlug, page);

        return Ok(page);
    }
}
