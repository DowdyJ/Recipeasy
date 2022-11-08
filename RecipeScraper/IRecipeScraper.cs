using PuppeteerSharp;

namespace Recipeasy.RecipeScraper;

public interface IRecipeScraper 
{
    public abstract Task<Recipe> ScrapeRecipeAsyc(IPage page,String url, RecipeScraperOptions scraperOptions);
}