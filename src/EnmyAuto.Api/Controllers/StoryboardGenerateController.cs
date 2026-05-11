using EnmyAuto.Api.Data;
using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Models;
using EnmyAuto.Api.Services.Ai;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EnmyAuto.Api.Controllers;

[ApiController]
[Route("api/storyboards")]
public sealed class StoryboardGenerateController(
    IAiStoryboardService aiService,
    ApplicationDbContext db) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateStoryboardRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var script = await aiService.GenerateStoryboardAsync(
            request.ProductName,
            request.Category,
            cancellationToken);

        // Persist the storyboard so it can be referenced by the render job.
        var storyboard = new Storyboard
        {
            // TODO: replace with real authenticated UserId from JWT claims
            UserId     = Guid.Empty,
            Title      = script.Title,
            Category   = Enum.TryParse<StoryboardCategory>(request.Category, out var cat)
                             ? cat
                             : StoryboardCategory.ProductReview,
            ScriptJson = JsonSerializer.Serialize(script),
            Status     = StoryboardStatus.Draft,
        };

        db.Storyboards.Add(storyboard);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { storyboardId = storyboard.Id, script });
    }
}

public sealed record GenerateStoryboardRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MaxLength(120)]
    string ProductName,

    [property: System.ComponentModel.DataAnnotations.Required]
    string Category);
