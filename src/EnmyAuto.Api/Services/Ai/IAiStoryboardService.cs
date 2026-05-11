using EnmyAuto.Api.Models.Ai;

namespace EnmyAuto.Api.Services.Ai;

public interface IAiStoryboardService
{
    /// <summary>
    /// Calls the OpenAI API and returns a fully-parsed storyboard script
    /// for the given product and category.
    /// </summary>
    Task<StoryboardScript> GenerateStoryboardAsync(
        string productName,
        string category,
        CancellationToken cancellationToken = default);
}
