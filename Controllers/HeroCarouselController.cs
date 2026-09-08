using EdgeTech.API.Data;
using EdgeTech.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EdgeTech.API.Controllers;

[ApiController]
[Route("api/hero-carousel")]
public class HeroCarouselController : ControllerBase
{
    private readonly MongoDbContext _db;

    public HeroCarouselController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// The slides shown before an admin has ever saved the carousel. Mirrors the
    /// images that shipped with the site so the homepage looks unchanged on first run.
    /// </summary>
    private static List<HeroSlide> DefaultSlides() => new()
    {
        new HeroSlide
        {
            ImageUrl = "/1.png",
            Title = "Secure Your World\nWith Smart Surveillance",
            Subtitle = "Professional-grade CCTV systems trusted by thousands across Bangladesh",
            Cta = "Shop CCTV Cameras",
            CtaLink = "/products?category=analog-cameras",
            Order = 0
        },
        new HeroSlide
        {
            ImageUrl = "/2.png",
            Title = "Build Your Custom\nSolution",
            Subtitle = "Configure your perfect surveillance and IT setup with our interactive solution builder",
            Cta = "Build Your Solution",
            CtaLink = "/package-builder",
            Order = 1
        },
        new HeroSlide
        {
            ImageUrl = "/3.png",
            Title = "Enterprise Networking\nSolutions",
            Subtitle = "Switches, routers, and complete networking infrastructure for any scale",
            Cta = "Explore Networking",
            CtaLink = "/products?category=networking",
            Order = 2
        },
        new HeroSlide
        {
            ImageUrl = "/4.png",
            Title = "Complete Office\nInfrastructure Stack",
            Subtitle = "Servers, network switches, storage, and deployment-ready enterprise equipment",
            Cta = "Shop Infrastructure",
            CtaLink = "/products?category=storage",
            Order = 3
        }
    };

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var carousel = await _db.HeroCarousels.Find(_ => true).FirstOrDefaultAsync();

        if (carousel == null)
        {
            carousel = new HeroCarousel { Slides = DefaultSlides(), UpdatedAt = DateTime.UtcNow };
            await _db.HeroCarousels.InsertOneAsync(carousel);
        }

        carousel.Slides = carousel.Slides.OrderBy(s => s.Order).ToList();
        return Ok(carousel);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] HeroCarousel request)
    {
        var slides = request.Slides ?? new List<HeroSlide>();

        // Every slide is an image; a slide without one would render an empty frame.
        slides = slides.Where(s => !string.IsNullOrWhiteSpace(s.ImageUrl)).ToList();

        if (slides.Count < HeroCarousel.MinSlides)
        {
            return BadRequest(new
            {
                message = $"The carousel needs at least {HeroCarousel.MinSlides} slides with images."
            });
        }

        // Renumber from the submitted order so the client never has to keep Order in sync.
        for (var i = 0; i < slides.Count; i++)
        {
            slides[i].Order = i;
            if (string.IsNullOrWhiteSpace(slides[i].Id))
                slides[i].Id = Guid.NewGuid().ToString("N");
        }

        var existing = await _db.HeroCarousels.Find(_ => true).FirstOrDefaultAsync();

        var updated = new HeroCarousel
        {
            Id = existing?.Id ?? string.Empty,
            Slides = slides,
            AutoplayMs = request.AutoplayMs is >= 2000 and <= 30000 ? request.AutoplayMs : 6000,
            UpdatedAt = DateTime.UtcNow
        };

        if (existing == null)
        {
            await _db.HeroCarousels.InsertOneAsync(updated);
        }
        else
        {
            await _db.HeroCarousels.ReplaceOneAsync(c => c.Id == existing.Id, updated);
        }

        return Ok(updated);
    }
}
