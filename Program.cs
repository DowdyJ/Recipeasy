
using PuppeteerSharp;
using Recipeasy.RecipeScraper;
using Recipeasy.RecipePrinter;

namespace Recipeasy;

class Program 
{
    static async Task Main (String[] args)
    {
        String url = args[0];

        using BrowserFetcher browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true
        });
        using IPage page = await browser.NewPageAsync();

        await page.GoToAsync(url);

        IRecipeScraper scraper = new WprmRecipeScraper();
        Recipe recipe = await scraper.ScrapeRecipeAsyc(page, url, RecipeScraperOptions.Ingredients);

        IRecipePrinter recipePrinter = new ConsoleRecipePrinter();
        recipePrinter.PrintRecipe(recipe);
        
        await browser.CloseAsync();
        return;
    }
}
